# ADR-0011: Follows as Blogroll State — Issue-Driven Entries, `u-follow-of`, and Follow Webmentions

## Status
Accepted — 2026-10-30. Implementation detail lives in
[`docs/follow/ARCHITECTURE-OVERVIEW.md`](../follow/ARCHITECTURE-OVERVIEW.md).

Related: [ADR-0010](0010-websub-publisher-and-hub.md) amendment ("hub is not a follower list") documents
why follow signals must be explicit rather than inferred from feed subscriptions.

## Context

The site already publishes *who it follows*: `Data/blogroll.json` and `Data/podroll.json` render at
`/collections/{blogroll,podroll}/`, as OPML at `.../index.opml`, and are included from
`/.well-known/following.opml`. Until now this was one-directional — the followed sites were never told.

The IndieWeb has a lightweight, receiver-optional way to say "I follow you": an
[`h-entry` with `u-follow-of`](https://indieweb.org/follow) sent as a plain
[Webmention](https://indieweb.org/Webmention). Receivers that understand `follow-of` (webmention.io,
Bridgy Fed, several CMSs) classify it as a follow; receivers that don't simply see a mention from the
blogroll page. Either outcome is acceptable.

Two design questions had to be settled:

1. **Where does the follow live?** A per-follow *post* (`_src/follows/*.md`) would mirror how responses
   and bookmarks work, but a follow is **state, not an event**. Keeping a post around after unfollowing is
   wrong; deleting it loses the source page the receiver uses to verify removal. The blogroll already *is*
   the state.
2. **What is the target — the site or the feed?** Webmention endpoint discovery and `u-follow-of`
   semantics are HTML-only. The feed URL is how *I* get updates; the site URL is who I follow. A roll
   without a working feed is pointless to the author, so `XmlUrl` stays required even though it is never
   the follow target.

## Decision

1. **The roll entry is the follow.** No follow posts. Each entry on `/collections/blogroll/` and
   `/collections/podroll/` renders as an `h-entry` whose `u-follow-of` is an `h-card` linking to
   `HtmlUrl`; `dt-published` comes from a new optional `Added` field (`YYYY-MM-DD`). The page is an
   `h-feed` with a hidden `p-author h-card`. Other rolls (YouTube, forums, starter packs) are unchanged.
2. **Entries are added through a GitHub issue form** (`follow.yml`, label `follow`) mirroring the existing
   content-issue pipeline: `Scripts/process-follow-issue.fsx` validates both URLs, normalizes the site URL,
   performs **one defensive GET** on the feed (reachable and sniffs as RSS/Atom/JSON Feed — no
   autodiscovery; the form already carries the fields), rejects duplicates across both rolls, appends the
   entry with `Added = today`, and the workflow opens a PR. The RSS browser extension's custom-reader URL
   can prefill `feed_url`.
3. **Follow and unfollow webmentions are sent on publish from a roll diff.**
   `Scripts/identify-follow-webmentions.fsx` compares `HtmlUrl` membership of both rolls between the last
   successfully deployed commit and the current one. Added *and* removed URLs each get
   `source=https://lqdev.me/collections/{roll}/ target=HtmlUrl`. Removal is sent by default (the receiver
   re-fetches the source, finds no link, and drops the follow). Field-only edits never notify.
4. **Historical entries are backfilled, not announced.** `Scripts/backfill-roll-dates.fsx` derives `Added`
   from `git log -S` (25 entries sit on the 2022-08-14 file-creation floor). The backfill commit changes
   no `HtmlUrl`, so the diff step sends nothing. A deliberate one-time announce exists as a manual
   `--all` flag and is never invoked by CI.

## Consequences

**Easier**
- Following someone is a one-form action that lands in the roll, the OPML, `following.opml`, and the
  followed site's webmention inbox, all from one data change.
- Unfollowing is deleting a JSON object; the next publish tells the receiver.
- Follow dates give the rolls a timeline and make `dt-published` meaningful.

**Harder / accepted**
- Roll JSON files are now rewritten by scripts; the first rewrite normalizes legacy indentation.
- The base-commit lookup depends on `gh run list` seeing the deploy workflow; it falls back to the push's
  previous SHA, and with no base at all (manual dispatch) it sends nothing rather than guessing.
- Receivers that ignore `follow-of` see a generic mention from a page that lists dozens of sites. That is
  the honest truth of the page and is within the spec.
- A feed that blocks bots (403/429) passes the defensive check with a warning rather than failing, so a
  bad feed URL behind a WAF can still be followed. The failure mode is "no updates", visible immediately.

**Out of scope (tracked in `projects/backlog.md`)**
- YouTube roll, `Data/feeds.json` (own feeds), and any autodiscovery.
- Changes to the browser extension itself (secondary "Follow" action, `%u`/`%t` placeholders).
- A "new follows" feed and a follower-side (`/followers`) view.
