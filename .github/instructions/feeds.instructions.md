---
applyTo: "UnifiedFeeds.fs,GenericBuilder.fs,Collections.fs,Processors/*Processor.fs,Services/FeedSemantics.fs,Services/Opml.fs,Builders/LegacyFeeds.fs,test-scripts/test-rss*.fsx"
---

# Feeds

These files build the RSS feeds and their items, the OPML files, and the tests that guard them. Each content processor's `RenderRss` builds that type's feed items.

- Put a feed at `/TYPE/feed.xml`. Friendly names such as `/blog.rss` are redirects in `staticwebapp.config.json`, not separate files.
- Keep existing item elements, attributes, and their order, because feed readers depend on them. No CI test enforces this. Before the change, build and run `dotnet fsi test-scripts/test-rss-semantics.fsx --capture-fixtures DIR`. After it, build again and run the script with `--baseline-fixtures DIR`. That comparison covers post, note, and response items only.
- Emit descriptions as real CDATA sections through `rssDescriptionElement`, which also handles content that contains `]]>`. An escaped CDATA string is a bug that `test-scripts/test-rss-cdata.fsx` catches.
- Run all five `test-rss-*` scripts after any feed change, after `dotnet build` and a generate. The list is in `.github/workflows/validate-site-upgrade.yml`.

Read first: `docs/reference/feeds.md`
