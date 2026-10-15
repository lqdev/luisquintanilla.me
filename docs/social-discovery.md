# Social discovery endpoints

The site publishes the profile and subscription artifacts it already maintains at the stable locations recommended by [SocialSpec](https://socialspec.org/). The relevant FollowSpec, AboutSpec, and ContactSpec documents are draft specifications; these endpoints are intended for machine-facing discovery by readers, social tools, and other sites.

## Endpoints

| Endpoint | Source | Response |
| --- | --- | --- |
| `/.well-known/following.opml` | `Data/blogroll.json` through the generated blogroll collection | OPML 2.0 with `title`, `text`, `type`, `htmlUrl`, and `xmlUrl` attributes |
| `/.well-known/about.md` | `_src/about.md` | Markdown with YAML frontmatter and the About body |
| `/.well-known/contact.vcf` | `_src/vcard.vcf` | vCard 3.0 |
| `/.well-known/profile` | `_src/.well-known/profile.json` | Versioned site-specific JSON manifest |

The following endpoint intentionally publishes the public blogroll, not `/feed/index.opml`. The latter is a list of Luis's own feeds and is therefore not a following list.

Azure Static Web Apps rewrites the following, contact, and extensionless profile endpoints to their existing generated artifacts. About Markdown is copied as a generated artifact because its machine-readable `summary` frontmatter must remain available. The `mimeTypes` map supplies the required `.opml`, `.md`, `.vcf`, and `.json` media types; endpoint routes supply `Access-Control-Allow-Origin: *` without changing global CORS or fallback behavior.

`/.well-known/profile` is a stable compatibility alias for `/.well-known/profile.json`. The extensionless URL is useful to consumers that want a predictable profile location, while the versioned JSON filename lets Static Web Apps infer `application/json` reliably.

## Profile manifest

The profile manifest is deliberately a documented site-specific contract, not a new internet standard. It provides one machine-readable index of the established identity and social surfaces already hosted by the site:

- profile name, canonical URL, About artifact, contact artifact, and avatar
- ActivityPub accounts and `rel=me` links for GitHub and Bluesky
- RSS feed URLs
- WebFinger, Webmention, ActivityPub, and IndieAuth service URLs
- OPML following lists and the existing starter pack

The manifest declares JSON Schema 2020-12 and a versioned schema identifier so consumers can validate and evolve their integration without confusing this convenience document with WebFinger, ActivityPub, IndieAuth, or another formal discovery protocol.

## About Markdown

`_src/about.md` remains the single source of truth for both the human About page and the portable About artifact. It now requires a YAML frontmatter block with a non-empty `summary`:

```markdown
---
summary: "Luis Quintanilla is a Chicago-based creator working on machine learning and AI for .NET at Microsoft, with interests in the open and decentralized web."
---

# Hi!

The human-readable About content follows.
```

The summary is metadata for consumers of `/.well-known/about.md`. It is removed before the source is rendered into `/about/` and the text-only About page, so frontmatter is not displayed as visible page content.

The About parser is intentionally separate from the global Markdown parser. Enabling YAML frontmatter globally would change the handling of existing content types, so only the About source has the required metadata contract.

## Feed discovery

The shared HTML layout advertises `/feed/feed.xml` as the first RSS alternate link. Existing `/blog.rss`, `/microblog.rss`, and `/responses.rss` links remain in place. This provides a single default feed for consumers while preserving existing subscriptions; see [Feed Architecture](feed-architecture.md).

No `rel="following"` link is emitted. SocialSpec's published discovery flow uses a direct GET to `/.well-known/following.opml`, and the relation is not required by the current published guidance.

## Validation

Run the generator and focused artifact checks from the repository root:

```powershell
dotnet build --no-restore
dotnet run --no-build
dotnet fsi .\test-scripts\test-social-discovery.fsx
```

The focused script validates:

- About summary parsing, body-only rendering, source parity, and invalid metadata rejection
- OPML 2.0 validity, required `text` attributes, collection counts, and XML escaping
- vCard version, `FN`, and source parity
- Profile manifest schema/version, canonical URLs, service links, and typo regression
- Static Web Apps routes, rewrites, extension-based MIME mappings, CORS, and `/.well-known/*` fallback exclusion
- Homepage h-card identity fields and page-specific Open Graph/Twitter metadata
- Unified RSS autodiscovery and preservation of the existing feed links

After deployment, pass the site origin to run HTTP smoke tests for GET, HEAD, CORS, media types, redirects, and unknown well-known paths:

```powershell
dotnet fsi .\test-scripts\test-social-discovery.fsx --base-url https://www.luisquintanilla.me
```

The local checks do not require network access. The `--base-url` checks must be run against the deployed site because Static Web Apps rewrites are not reproduced by the local generator.

## Related infrastructure

These endpoints complement, rather than replace, the site's existing identity and federation surfaces:

- `/.well-known/webfinger` for ActivityPub actor discovery
- IndieAuth metadata and authorization endpoints
- ActivityPub actor, inbox, outbox, and WebMention endpoints
- AT Protocol/Bluesky publishing infrastructure
- Microformats2 and Schema.org identity metadata in rendered HTML

The endpoint contract is configured in `staticwebapp.config.json`. Do not edit `_public` directly; regenerate it with `dotnet run`.
