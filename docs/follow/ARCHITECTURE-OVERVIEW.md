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
issue (template follow.yml, label follow, author lqdev)
  └─ .github/workflows/process-content-issue.yml :: process-follow
       ├─ extract Feed URL / Site URL / Title / Roll from the form body
       ├─ dotnet fsi Scripts/process-follow-issue.fsx -- feed site title roll
       │    1. both URLs absolute http(s)
       │    2. site URL normalized via Uri.AbsoluteUri (lowercase host, trailing slash on bare origin)
       │    3. duplicate check across BOTH rolls on normalized HtmlUrl or XmlUrl
       │       ("Already following '<title>' in <roll> (added YYYY-MM-DD)")
       │    4. one defensive GET on the feed URL (20 s, UA set, 8 KB read):
       │         2xx + sniff <rss | <feed | <rdf:RDF | JSON with jsonfeed.org  → ok
       │         2xx but not feed-like                                        → fail
       │         404 / 410 / DNS / connection error / timeout                 → fail
       │         401 / 403 / 429 / 5xx                                        → warn, proceed
       │    5. append {Title, Type:"rss", HtmlUrl, XmlUrl, Added: today(America/New_York)}
       │       written with 4-space indent, relaxed escaping, trailing newline
       ├─ peter-evans/create-pull-request → branch content/issue-N/follow
       └─ comment + close issue (success) | comment + fail job (error)
```

No PersonalSite.dll dependency, so the job does not build the site. Workflow triggers are
`opened`/`reopened`; to retry after an error, edit the issue and close/reopen it.

**Browser extension prefill.** Set the custom reader URL in
[`lqdev/rss-browser-extension`](https://github.com/lqdev/rss-browser-extension) to
`https://github.com/lqdev/luisquintanilla.me/issues/new?template=follow.yml&feed_url=%s`.
GitHub prefills form fields by id from the query string. Site URL and Title are typed in by hand.
Extension-side changes (a second "Follow" action, `%u`/`%t` placeholders) are deferred.

**Local test:** `FOLLOW_SKIP_FEED_CHECK=1 dotnet fsi Scripts/process-follow-issue.fsx -- <feed> <site> <title> blogroll`.

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
