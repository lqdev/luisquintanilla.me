# Follow via Issue → Blogroll → Webmention

Decision record: [ADR-0011](../adr/0011-follows-as-blogroll-state.md).

## 1. Data

`Data/blogroll.json` and `Data/podroll.json` are arrays of:

```json
{
  "Title": "Example Blog",
  "Type": "rss",
  "HtmlUrl": "https://example.com/",
  "XmlUrl": "https://example.com/feed.xml",
  "Added": "2026-10-30"
}
```

| Field | Role |
|-------|------|
| `HtmlUrl` | **Follow target.** Webmention target and `u-follow-of` href. |
| `XmlUrl` | How updates arrive. Required; never the follow target (endpoint discovery is HTML-only). |
| `Added` | Optional `YYYY-MM-DD`; rendered as `dt-published`. Backfilled from git history for pre-existing entries (25 of 46 sit on the 2022-08-14 file-creation floor). |

Entries are appended in human order; the files are not sorted. Loading happens in
`Collections.fs` (`loadCollectionData`), which deserializes straight into `CollectionItem`
(System.Text.Json maps a missing `Added` to `None`).

Only `blogroll` and `podroll` are follow collections (`Collections.followCollectionIds`). YouTube,
forums, and starter packs keep plain markup and are not diffed.

## 2. Markup (`Collections.generateStandardCollectionPage`)

```html
<div class="mr-auto h-feed">
  <h2 class="p-name">Blogroll</h2>
  <div class="p-author h-card microformat-hidden">…Luis Quintanilla…</div>
  <p>… <a href="/collections/blogroll/index.opml" rel="alternate" type="text/x-opml">OPML file</a> …</p>
  <ul>
    <li class="h-entry">
      <strong><a class="u-follow-of h-card" href="https://example.com/">Example Blog</a></strong>
      - <a href="https://example.com/feed.xml" type="application/rss+xml">RSS Feed</a>
      · <small>following since <time class="dt-published" datetime="2026-10-30">Oct 30, 2026</time></small>
    </li>
  </ul>
</div>
```

The anchor text is the site title so the *implied* h-card name/url are correct; no `u-url`/`p-name`
are put on the same element (they would attach to the h-entry, not the h-card). Verified with
`microformats-parser`: each child parses as `{"follow-of":[{"type":["h-card"],"properties":{"name":[…],"url":[…]}}],"published":[…]}`.

## 3. Adding a follow (issue pipeline)

```
Android share sheet → github-post PWA Follow action
  └─ issue (follow.yml; feed_url optional, site_url/site_title/roll required)
       └─ process-content-issue.yml :: process-follow
            └─ .github/actions/process-follow/action.yml
                 └─ Scripts/process-follow-issue.fsx -- feed site title roll
                      ├─ exit 0: one verified feed → append + PR + close issue
                      ├─ exit 1: hard error → comment; leave issue open
                      ├─ exit 2: many feeds → marked checklist appended to issue body
                      │          └─ process-follow-choice.yml
                      │               ├─ lqdev edits issue; exactly one [x] → selected URL
                      │               └─ lqdev comments /follow N or /follow URL → selected URL
                      │                    └─ re-enter the same composite action
                      └─ exit 3: no feed → comment + close "not planned"; no data/branch/PR
```

The issue form's field ids are `feed_url`, `site_url`, `site_title`, and `roll`; `site_title` keeps the
visible label and generated body heading **Title**. All untrusted values enter the composite action as
environment values before the F# script is invoked.

### Discovery and validation

1. Validate Site URL, Title, and Roll. Normalize the Site URL with `Uri.AbsoluteUri`.
2. Read both rolls and reject a duplicate normalized `HtmlUrl` before making any discovery request.
3. If `feed_url` is present, validate that URL and continue only if the defensive check passes.
4. Otherwise GET the Site URL. If the URL itself is RSS/Atom/JSON Feed, it is the one candidate.
5. For a static HTML response, inspect `link[rel="alternate"]` and `a` elements with
   `application/rss+xml`, `application/atom+xml`, or `application/feed+json`. Resolve relative `href`
   values against the document or first `<base href>`.
6. Add `/feed`, `/feed/`, `/rss.xml`, `/atom.xml`, `/index.xml`, `/feed.xml`, `/rss`, and
   `/feed.json` probes.
7. Deduplicate candidates. Re-run the defensive GET on each candidate:
   - 2xx + `<rss`, `<feed`, `<rdf:RDF`, or JSON containing `jsonfeed.org` → valid
   - 2xx but not feed-like, 404, 410, DNS/connection error, or timeout → reject candidate
   - 401/403/429/5xx → warn and accept, preserving the original defensive-check behavior
8. Sort comment feeds after primary feeds and cap the list at 15. Zero exits 3, one exits 0, and
   multiple produce `CANDIDATES_JSON` and exit 2.
9. On exit 0, append `{Title, Type:"rss", HtmlUrl, XmlUrl, Added}` with 4-space indentation.
   `process-follow-issue.fsx` never writes unless a non-empty feed has passed the check.

The processor has no `PersonalSite.dll` dependency. The initial workflow trigger is `opened`/`reopened`;
reopening after an error remains the supported full retry. The choice workflow accepts `issues.edited`
from lqdev for checklist changes and `issue_comment.created` from lqdev for `/follow` commands; the
Action's own issue-body edit is excluded because its sender is not lqdev. Zero checked items is a no-op,
while multiple checked items receive an "exactly one" correction comment. Initial processing and both
choice paths share `concurrency: follow-issue-<number>` so repeated deliveries do not race.

JavaScript-injected links are invisible to Action-side static discovery. A manual `feed_url` remains the
escape hatch. No hosted discovery service or third-party proxy participates.

**Desktop extension prefill.** The custom reader URL in
[`lqdev/rss-browser-extension`](https://github.com/lqdev/rss-browser-extension) is
`https://github.com/lqdev/luisquintanilla.me/issues/new?template=follow.yml&feed_url=%s`.
This is the existing desktop handoff; the separate Follow action workstream will prefill Site URL and
Title as well as the feed. Android Chrome/Brave uses the PWA share target; there is no mobile extension
flow.

**Focused validation:**

- `node test-scripts/test-follow-choice.js`
- `node test-scripts/test-follow-processing.js`
- `dotnet fsi test-scripts/test-follow-yaml.fsx` (after `dotnet build`)

## 4. Sending follow / unfollow webmentions (publish pipeline)

In `.github/workflows/publish-azure-static-web-apps.yml`, `build_and_deploy_job`:

```
Identify follow webmentions (roll diff)
  BASE_SHA = headSha of the last *successful* run of this workflow on main   (gh run list)
             ‖ github.event.before                                            (fallback)
  no base → dotnet fsi Scripts/identify-follow-webmentions.fsx --            (writes [])
  base    → git fetch --depth=1 origin $BASE_SHA (if missing)
            dotnet fsi Scripts/identify-follow-webmentions.fsx -- --base $BASE_SHA
→ _tmp/follow-webmentions.json  (uploaded with _tmp/webmentions.json as webmention-artifacts)
```

`send_webmentions_job` → `Scripts/send-webmentions.fsx` loads both files and sends with
`lqdev.WebmentionFs` after the deploy.

The diff is on `HtmlUrl` set membership per roll:

| Change | Webmention |
|--------|-----------|
| `HtmlUrl` appears | `source=https://lqdev.me/collections/{roll}/ target=<HtmlUrl>` (follow) |
| `HtmlUrl` disappears | same pair (receiver re-fetches source, link is gone → unfollow). Disable with `--no-removals`. |
| `Title`/`XmlUrl`/`Added` edits, reformatting | nothing |
| base file missing at `BASE_SHA` | nothing, with a warning (never mass-send on a bad base) |
| `--all` (manual only) | one webmention per current entry — a deliberate announce, not wired to CI |

Using the last successful run as the base means a deploy cancelled by `concurrency.cancel-in-progress`
does not lose its follows; the next run diffs across both commits.

## 5. Unfollow

Delete the object from the roll file in a PR. On publish the diff step emits the same
source/target pair; the receiver re-verifies the source page, finds no link to its URL, and
removes the follow. There is no 410 or special "unfollow" post.

## 6. Verification checklist

- `dotnet build` / `dotnet run`; inspect `_public/collections/blogroll/index.html` for `h-feed`,
  `h-entry`, `u-follow-of h-card`, `dt-published`, and `rel="alternate" type="text/x-opml"`.
- `dotnet fsi Scripts/identify-follow-webmentions.fsx -- --base HEAD` on a backfill-only change → `Wrote 0`.
- `dotnet fsi Scripts/backfill-roll-dates.fsx` twice → second run backfills 0 (idempotent).
- `dotnet fsi test-scripts/test-social-discovery.fsx` → OPML counts unchanged.
