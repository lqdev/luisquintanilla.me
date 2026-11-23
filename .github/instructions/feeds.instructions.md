---
applyTo: "UnifiedFeeds.fs,Services/FeedSemantics.fs,Services/Opml.fs,Builders/LegacyFeeds.fs,test-scripts/test-rss*.fsx"
---

# Feeds

These files build the RSS feeds, the OPML files, and the tests that guard them.

- Put a feed at `/TYPE/feed.xml`. Friendly names such as `/blog.rss` are redirects in `staticwebapp.config.json`, not separate files.
- Keep existing elements, attributes, and their order. Feed readers depend on them, and `test-scripts/test-rss-semantics.fsx` compares items against baseline fixtures.
- Emit descriptions as real CDATA sections through `rssDescriptionElement`, which also handles content that contains `]]>`. An escaped CDATA string is a bug that `test-scripts/test-rss-cdata.fsx` catches.
- Run all five `test-rss-*` scripts after any feed change, after `dotnet build` and a generate. The list is in `.github/workflows/validate-site-upgrade.yml`.

Read first: `docs/reference/feeds.md`
