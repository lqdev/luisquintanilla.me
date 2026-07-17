---
title: "Pattern: Making a Static Site an AT Protocol Node — CI-Provisioned Publication + .well-known Verification Traps"
description: "How to add a Standard.site publication to a static site (Azure SWA) with a one-time CI-run provisioning script and a static verification endpoint — plus the SPA-fallback, byte-format, write-only-secret, and default-branch gotchas."
entry_type: pattern
published_date: "2026-07-16 18:50 -05:00"
last_updated_date: "2026-07-16 18:50 -05:00"
tags: "atproto, bluesky, standard-site, azure, static-site, devops, patterns"
related_skill: ""
source_project: "lqdev-me"
related_entries: research-at-protocol-static-site-integration, pattern-atproto-tid-record-keys-sourcehash-workaround, pattern-long-lived-umbrella-branch-merge-strategy
---

## Discovery

lqdev.me is a static site (F# generator → Azure Static Web Apps) that already federates over ActivityPub
with a "static hub, thin dynamic spokes" split (~99% static files, ~1% Azure Functions). The goal was to
make the *same* site a first-class node in the ATmosphere (AT Protocol) using the community
[Standard.site](https://standard.site/) Lexicons — which now get an enhanced render in the Bluesky
timeline — **without running a PDS, relay, or AppView**.

Standard.site models a website as one `site.standard.publication` record plus per-post
`site.standard.document` records. "Part A" is deceptively small: create the publication record *once*,
and prove the domain owns it. On a static host driven by CI, that one authenticated write plus its static
verification endpoint hid four non-obvious traps.

## Root Cause

Everything about a static-site AT Protocol node is static reads — *except* the one-time provisioning,
which is an authenticated mutation against a PDS. All the friction lives in the seams between "one
authenticated write" and "static hosting + CI + a secret":

1. **The write needs a secret, but GitHub Actions secrets are write-only.** `gh secret list` shows names
   and timestamps only; nothing (not the API, not the repo owner) can read the value back. So you can't
   "just run the create script locally with the secret" unless you paste the credential into your shell.

2. **`workflow_dispatch` only dispatches from the *default* branch.** A multi-phase integration otherwise
   lives entirely on a feature branch, but the one-time provisioning workflow can't be triggered there —
   it has to land on `main` first.

3. **Static hosts with SPA fallback make verification endpoints *lie*.** Azure SWA's `navigationFallback`
   rewrites any unmatched path to `/index.html` with a `200`. So a not-yet-deployed `.well-known`
   endpoint returns `200` **plus your entire homepage HTML**, not a `404`. A naïve "is it 200?" check
   passes on garbage.

4. **The endpoint's exact byte format matters and isn't pinned down in prose.** Trailing newline? BOM?
   Content-Type? You have to match a *working* reference, not a spec paragraph.

## Solution

Structure it as: (1) a one-time idempotent provisioning script run *inside CI*, (2) a static verification
file emitted by the normal build, (3) verification by response **body**, not status.

### 1. One-time provisioning as a `workflow_dispatch` job (secret stays in GitHub)

Because the secret is write-only, don't run the create locally. Store the Bluesky **app password** as a
repo secret and run the create script inside a manual workflow — the *public* AT-URI comes out in the run
log; the credential never prints.

```yaml
# .github/workflows/atproto-create-publication.yml  (manual-only)
on: { workflow_dispatch: {} }
permissions: { contents: read }
jobs:
  create:
    runs-on: ubuntu-latest
    env:
      ATPROTO_APP_PASSWORD: ${{ secrets.ATPROTO_APP_PASSWORD }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet fsi Scripts/create-atproto-publication.fsx
```

Make the script **idempotent** — resolve the PDS from the DID doc, list existing publications first, and
exit with the existing AT-URI if one already exists, so re-running is always safe:

```fsharp
// resolve PDS dynamically from https://plc.directory/{did}  (service AtprotoPersonalDataServer)
// then:
let existing = getJson (pds + "/xrpc/com.atproto.repo.listRecords?repo=" + did
                            + "&collection=site.standard.publication")
match existing.records with
| xs when xs.Length > 0 -> printfn "PUBLICATION_URI=%s" xs.[0].uri; exit 0   // duplicate guard
| _ ->
    let pw = Environment.GetEnvironmentVariable "ATPROTO_APP_PASSWORD"       // read ONLY when writing
    let jwt = createSession did pw
    // createRecord with validate:false so an unknown-lexicon write stores as validationStatus:"unknown"
    let uri = createRecord jwt did "site.standard.publication" publicationValue
    printfn "PUBLICATION_URI=%s" uri
```

Custom `site.standard.*` records write fine to a **Bluesky-hosted PDS** (no self-host) with an app
password and `validate:false`. This one workflow file must be merged to the **default branch** before it
can be dispatched — even when everything else is on a feature branch, this inert file goes to `main`
first (trap #2).

### 2. Static verification endpoint, emitted by the build

Publication verification per the [standard.site spec](https://standard.site/docs/verification/) is a
`/.well-known/site.standard.publication` endpoint that returns the record's AT-URI. On this generator,
dropping the file in `_src/.well-known/` is enough — the asset step copies `.well-known/` verbatim into
`_public/`.

**Match a known-working reference byte-for-byte.** rednafi.com is a Bluesky-timeline-verified static
publication:

```powershell
curl.exe -s https://rednafi.com/.well-known/site.standard.publication
# 200 · Content-Type: application/octet-stream · length 77 · last byte 'z' (NO trailing newline)
```

So the file is the **bare AT-URI, no trailing newline, no UTF-8 BOM**:

```
at://did:plc:pme7qquljcdx6i4zyawoxypd/site.standard.publication/3mqs7sgylil2w
```

`application/octet-stream` is fine — verifiers fetch the body server-side and read it as text. No trailing
newline is *strictly safer* than one (a naïve non-trimming verifier would fail on the extra byte). Verify
the emitted bytes, not just that a file exists:

```powershell
$b = [IO.File]::ReadAllBytes("_public/.well-known/site.standard.publication")
$b.Length          # 77
$b[0],$b[1],$b[2]  # 97,116,58 = 'a','t',':'  (a BOM would be 239,187,191)
$b[-1]             # 119 = 'w'  (NOT 10 = newline)
```

### 3. Verify by BODY, not status — the SPA-fallback trap

Immediately after the merge/deploy the endpoint returned `200` — but it was the fallback:

```
STATUS=200  CT=text/html  LEN=3489610     # 3.4 MB = the homepage, not the AT-URI
```

A **real file at the path takes precedence** over `navigationFallback` (that's exactly why the sibling
`/.well-known/profile` already serves correctly), so once the deploy finished it flipped to the truth:

```
STATUS=200  CT=application/octet-stream  LEN=77
BODY=[at://did:plc:pme7qquljcdx6i4zyawoxypd/site.standard.publication/3mqs7sgylil2w]
```

Then assert the **bidirectional handshake** — the record's `url` points at your domain *and* your
`.well-known` points at the record's AT-URI:

```powershell
$site = (curl.exe -s https://lqdev.me/.well-known/site.standard.publication).Trim()
$rec  = curl.exe -s "$pds/xrpc/com.atproto.repo.getRecord?repo=$did&collection=site.standard.publication&rkey=$rkey" | ConvertFrom-Json
($site -eq $rec.uri)                       # site  -> record : True
($rec.value.url -eq 'https://lqdev.me')    # record -> site  : True
```

## Prevention

- **On any SPA/static host, verify `.well-known` and other extensionless endpoints by response *body*,
  not status code.** Catch-all rewrites (`navigationFallback`, `try_files … /index.html`, Next.js
  fallbacks) turn "missing file" into "200 + index.html." Assert `Content-Type != text/html` **and**
  body-equals-expected. File-precedence means the endpoint self-heals once deployed; the body assertion
  is what tells you it actually did.
- **Match a working reference's exact bytes for protocol endpoints** — length, trailing newline, BOM,
  content-type — instead of trusting a prose spec. One `curl` against a known-good publisher settles it.
- **Run one-time authenticated provisioning inside a `workflow_dispatch` job, never locally.** GitHub
  secrets are write-only by design, so this keeps the credential in GitHub and surfaces only the *public*
  identifier in the log. Make the script idempotent (list-then-create) so re-runs can't create a second
  record.
- **`workflow_dispatch` dispatches only from the default branch** — the provisioning workflow (and only
  it) must land on `main` even when the rest of the integration lives on a feature branch. See the
  branching model in [[pattern-long-lived-umbrella-branch-merge-strategy]].
- This completes **Part A** (the site is a verified publication node, discoverable via `showInDiscover`).
  Part B — per-post `site.standard.document` records plus `<link rel="site.standard.document">` verification
  tags — carries its own record-key constraint documented in
  [[pattern-atproto-tid-record-keys-sourcehash-workaround]]. Overall option analysis (identity, hosting
  extent, build-vs-adopt) lives in [[research-at-protocol-static-site-integration]].
