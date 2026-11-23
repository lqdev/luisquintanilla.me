---
name: add-content-type
description: Use when adding a new content type (a new kind of page with its own folder under _src, like posts, notes, reviews, or marketplace) to lqdev/luisquintanilla.me, or when planning one. Covers the decisions to make first, every file to change in compile order, the string-keyed places the compiler won't catch, and how to verify.
---

# Add a content type to lqdev.me

A content type is a folder of Markdown under `_src/` that gets its own pages, an identity in
`ContentTypes.fs`, and a row in the content roster. The last one added, `marketplace` (commit
`8192fd5b`), touched 26 files. Use it as the worked example: `git --no-pager show 8192fd5b -- {file}`.

Response subtypes (reply, reshare, star, rsvp, bookmark) are not content types. If the new thing is
a kind of response, add a `response_type` value instead and stop here.

## Decide three things first

Write the answers down before you change code. Each one is controlled in `Program.fs`.

1. Does it appear on the home timeline? Set `InTimeline` in its row of `let contentRoster`. A type
   on the timeline also needs the timeline sites in step 12. See the Memex entry
   `_src/resources/ai-memex/pattern-standalone-content-type-search-not-firehose.md` for why most
   standalone types stay off.
2. Does it federate? ActivityPub is opt-out: every item in the all-content feed is published to the
   outbox unless its type is excluded in `let activityPubContent = ...` (just under the
   "ActivityPub Content Generation" banner). AT Protocol is opt-in: nothing reaches Bluesky unless
   code is added to `AtProtoBuilder.fs`, which is its own project (read `docs/atproto/README.md`).
3. Does it get its own feed and a place in the all-content feed? `InAllFeeds` in the roster puts it
   in search, the text-only site, the type-specific feeds, and the ActivityPub source. Its own feed
   is an entry in the `buildAllFeeds` config list in `UnifiedFeeds.fs`. The public fire-hose
   (`/feed/feed.xml`) and the tag feeds include it unless you add it to the two exclusion filters in
   `UnifiedFeeds.fs` (search for `ContentTypes.Marketplace` to find them).

`InBlogArchive` is for posts, notes, and responses only. Leave it `false`.

## Checklist, in compile order

Replace `{Type}` with the PascalCase name (`Quote`), `{type}` with the wire name (`quotes`), and
`{Types}` with the plural PascalCase used for the DU case (`Quotes`).

1. `Domain.fs`: add a `[<CLIMutable>]` details record with a `[<YamlMember(Alias="...")>]` on every
   field, and a wrapper record `{ FileName; Metadata; Content; MarkdownSource }`. If it has tags,
   implement `ITaggable` on the wrapper, and return the wire name from `ContentType`. Copy
   `MarketplaceDetails` and `MarketplaceListing`. Use `published_date` for the date key and an
   offset in the value (`2026-01-15 08:00 -05:00`).
2. `ContentTypes.fs`: add the `[<Literal>]` wire string, the `ContentType` case, and arms in
   `serialize`, `parse`, and `urlPrefix`. The build then fails with one `FS0025`, in
   `toJsonFeedContent` in `UnifiedFeeds.fs`. Add the case to the `convertMdToHtml` group if the body
   is Markdown, or to the raw `item.Content` group if it is already HTML.
3. `ASTParsing.fs`: add `parse{Type}FromFile` next to `parseMarketplaceFromFile`. It is one line.
4. `Processors/{Type}Processor.fs`: new file. Copy `Processors/MarketplaceProcessor.fs` and change
   the type, the parse call, the URLs, and the date field. It reads tags as a YAML array and builds
   its card with Giraffe. (`SnippetProcessor.fs` reads comma-separated tags and uses an old string
   helper, so don't copy it.) Copy the processor and the `build{Types}()` in step 9 from the same
   type, so that `Render` and `ItemView` agree on Markdown or HTML. `RenderRss` must emit a `link` of
   `Constants.Urls.canonical + "/{type}/{slug}"`; that link becomes the item's URL everywhere.
5. `PersonalSite.fsproj`: add `<Compile Include="Processors\{Type}Processor.fs" />` after
   `Processors\MarketplaceProcessor.fs` and before `UnifiedFeeds.fs`.
6. `UnifiedFeeds.fs`: add `convert{Types}ToUnified` using the `toUnified` helper (copy
   `convertAlbumCollectionsToUnified`), and add a feed entry to the `buildAllFeeds` config list
   if it gets its own feed. Add the exclusion filters if it stays out of the fire-hose.
7. `Views/CollectionViews.fs`: the index (listing) view. `Views/LayoutViews.fs`: the page view.
   `Views/Partials.fs`: re-export both, because the builders open `Partials`, not the view modules.
8. `Views/Navigation.fs`, only if it gets a menu entry: for a top-level entry, add
   `let private {type}Icon = navIcon [ "{svg path}" ]` and `link "/{type}" "{Label}" (Some {type}Icon)`
   in `desktopSections`. For a dropdown entry, add an `item` instead.
9. `Builders/ContentTypePages.fs`: a `build{Types}()` that calls `BuildDriver.buildContentType`.
   Copy `buildMarketplace`. `OutputDir` and `Slug` decide the page URL; keep them in line with
   `urlPrefix` and the processor's RSS link.
10. Tags: add a row to `let allTaggableContent` in `Program.fs`, and a `| "{type}" -> (prefix, displayName)`
    arm in `Builders/TagPages.fs`.
11. Text-only site: add `"{type}"` to the `contentTypes` list in `TextOnlyBuilder.fs`, a page title
    arm in `textOnlyContentTypePage` in `Views/TextOnly/Pages.fs`, and a link in that file's
    "Browse Content" list.
12. Timeline (only if `InTimeline = true`):
    - `Views/TimelineViews.fs`: a filter button (`data-filter`), and a badge label arm in the
      `content-type-badge` match.
    - `_src/js/timeline.js`: add the wire name to `contentTypes` in `loadRemainingContentByType`
      (without it, only the first screen of items ever loads) and a label in the badge map.
    - `_src/css/custom/timeline.css`: a `.content-type-badge[data-type="{type}"]` color, optional.
13. `Services/LinkPreview.fs`: a `displayType` arm, so link previews say "Quote" and not "quotes".
14. `Program.fs`:
    - `let {type}FeedData = build{Types}()` with the other builds.
    - `let {type}Unified = UnifiedFeeds.convert{Types}ToUnified {type}FeedData`.
    - A row in `let contentRoster` with the flags from your decisions.
    - The ActivityPub exclusion, if it doesn't federate.
    - A `descriptionEntries` line in `let descriptionsByPath`, if the type has a description field.
15. Feed alias: if it has its own feed, add a `/{type}.rss` redirect to `/{type}/feed.xml` in
    `staticwebapp.config.json`, next to `/reviews.rss`. (`Builders/LegacyFeeds.fs` copies a file
    for `marketplace.rss`; don't copy that pattern.)
16. Styles: `_src/css/custom/{type}.css`, and an `@import` in `_src/css/custom/main.css`.
17. Content: one file in `_src/{type}/` to test with.

Optional: list the new feed on the Subscribe page. That list is written by hand in
`Views/CollectionViews.fs` (search for `feed.xml`) and in the feeds page of `Views/TextOnly/Pages.fs`.

Optional, for publishing from a GitHub issue: `.github/ISSUE_TEMPLATE/post-{type}.yml`, a job in
`.github/workflows/process-content-issue.yml`, the label in `.github/workflows/sync-issue-labels.yml`,
and `Scripts/process-{type}-issue.fsx`. Copy the marketplace versions.

## Verify

Build and generate with the `build-validate` skill. Then check:

```powershell
Test-Path _public/{type}/index.html
Get-ChildItem _public/{type} -Directory | Select-Object -First 3 Name
Test-Path _public/{type}/feed.xml
Select-String -Path _public/text/content/{type}/index.html -Pattern '<h1>' | Select-Object -First 1
```

For a timeline type, also check that `_public/index.html` contains `data-filter="{type}"`, and
either `data-type="{type}"` or `id="remainingContentData-{type}"`. The page renders only the 50
newest items; older ones sit in that JSON script and `timeline.js` loads them. For a type
that doesn't federate, check that `_public/api/data/outbox/index.json` has no URL under `/{type}/`.

## Finish

- Add a row to `docs/reference/content-types.md` and change the count in its first sentence.
- If it has its own feed, add it to `docs/reference/feeds.md` and to the feed list in
  `docs/websub/ARCHITECTURE-OVERVIEW.md`.
- If it federates, update `FEDERATION.md`.

## Gotchas

- The literal in `ContentTypes.fs` is a wire value. It appears in feeds, URLs, timeline JSON, and
  tag keys, so it can never be renamed after the first publish.
- `FS0025` (incomplete match) is an error here, set by `<WarningsAsErrors>` in `PersonalSite.fsproj`.
  The error after step 2 is the compiler pointing at a site for you. Don't add a wildcard arm to
  silence it.
- The processor must compile after `GenericBuilder.fs` (it returns a `GenericBuilder.ContentProcessor`)
  and before `UnifiedFeeds.fs` (which converts its output).
- The string-keyed sites in steps 10 to 15 compile fine when you miss them. A missed one falls back
  to the raw wire name or, for `timeline.js`, silently drops items. Search for the wire name of an
  existing type (`"reviews"`) to find any new ones.
- `OutputPath` on the processor is not used by `BuildDriver`. The page path comes from `OutputDir`
  and `Slug`.
- ActivityPub delivery runs in the publish workflow after a merge. A type you forgot to exclude
  gets sent to followers. Decide federation before the first merge.
