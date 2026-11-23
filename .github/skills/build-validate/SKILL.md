---
name: build-validate
description: Use when you changed F# code, views, CSS, feeds, or workflows in lqdev/luisquintanilla.me and need to build, generate the site, run the regression tests, or check what was written to _public. Also use when a restore fails with a lock file or package hash error.
---

# Build and validate lqdev.me

Run every command from the repository root in PowerShell.

## Build and generate

Use a private copy of the lock file. A plain `dotnet restore --locked-mode` often fails on local
machines with a package hash mismatch (NU1403), and the fix is not to change the committed lock file.
CI restores with `--locked-mode` against the committed file and passes.

```powershell
$localLock = Join-Path $env:TEMP 'lqdev-local.lock.json'
Copy-Item packages.lock.json $localLock
dotnet restore --force-evaluate -p:NuGetLockFilePath=$localLock
dotnet build --no-restore -p:NuGetLockFilePath=$localLock
$env:GITHUB_TOKEN = ''; $env:ACTIVITYPUB_STORAGE_CONNECTION = ''
dotnet run --no-build -- --strict
```

Expect 0 errors and one `FS1104` warning from `ActivityPubBuilder.fs`. The build takes about
10 seconds and the run about a minute. The run ends with lines about link previews and the link
registry, and exits 0.

`--strict` turns content parse errors into a failed run (`Diagnostics.fs`). Clearing the two
variables skips AI entity extraction and follower sync, so the run needs no network or secrets.

## Run the tests that match your change

Most scripts read `_public/`, so generate first.

| You changed | Run |
| --- | --- |
| Feeds (`UnifiedFeeds.fs`, `Services/FeedSemantics.fs`, a processor's `RenderRss`) | every `test-scripts/test-rss-*.fsx` |
| AT Protocol code | `test-atproto-response-mapping.fsx`, `test-atproto-media.fsx` |
| Social discovery files or `staticwebapp.config.json` | `test-social-discovery.fsx` |
| Follow handling | `test-follow-yaml.fsx`, `node test-scripts/test-follow-choice.js`, `node test-scripts/test-follow-processing.js` |
| Markdown outside `_src/` | `test-docs-integrity.fsx` |
| Anything under `api/` | in `api/`: `npm ci`, then `npm test` |

Run a script with `dotnet fsi test-scripts/{name}.fsx`; a nonzero exit code is a failure. To run
all the RSS scripts:

```powershell
Get-ChildItem test-scripts/test-rss-*.fsx | ForEach-Object { dotnet fsi $_.FullName; "$($_.Name) exit=$LASTEXITCODE" }
```

The full list CI runs is in `.github/workflows/validate-site-upgrade.yml`; if this table and that
file disagree, the workflow wins.

## Check the output

Pages are written as a single line of HTML, so search the whole file, not line by line. Replace
`{slug}` with a folder name from `_public/posts/`.

```powershell
$html = Get-Content _public/posts/{slug}/index.html -Raw
[regex]::Matches($html, 'class="[^"]*\b(h-entry|p-name|dt-published|e-content)\b[^"]*"').Value | Select-Object -Unique
[regex]::Matches($html, '(?s)<script type="application/ld\+json">(.*?)</script>') | ForEach-Object { $j = $_.Groups[1].Value | ConvertFrom-Json; if ($j.'@graph') { $j.'@graph'.'@type' -join ',' } else { $j.'@type' } }
Select-Xml -Path _public/posts/feed.xml -XPath '/rss/channel/item[position()<=3]' | ForEach-Object { '{0} | {1} | {2}' -f $_.Node.title, $_.Node.pubDate, $_.Node.SelectSingleNode('link').InnerText }
(Get-ChildItem _public -Recurse -File).Count
```

The same commands work for other types: swap `posts` for `notes`, `responses`, `reviews`, and so on.
Compare before and after for the page you changed. Azure Static Web Apps allows 15,000 files;
`Scripts/check-site-sizes.sh` warns at 14,000.

## Gotchas

- Never commit `packages.lock.json` after a local restore. If `git status` shows it changed, run `git restore packages.lock.json`.
- `dotnet run` without `--no-build` restores against the committed lock file, which can fail or rewrite it. Always pass `--no-build` after the build above.
- An RSS item has two `link` children (the RSS one and an Atom `link`), so `$item.link` returns both. Use `SelectSingleNode('link')` as above.
- A post page carries two JSON-LD blocks: the site's `WebSite` and `Person`, then the page's own (`BlogPosting` for a post).
- `_public/` is generated and ignored by git. Never edit it; change the source and regenerate.
- An incomplete pattern match (`FS0025`) is an error in this project.
- If content doesn't appear, check that the file is in the right `_src/` folder and that its frontmatter is valid YAML (no tabs). The run prints the file that failed to parse; without `--strict` it drops that file and still exits 0.
- Don't validate with the publish workflow. It deploys and federates.
