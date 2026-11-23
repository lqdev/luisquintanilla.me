# Principles

Why the site is built the way it is.

This site is my digital home and my identity on the web. I publish here first, and from here the
same posts reach people through RSS, the Fediverse, and Bluesky. I try to have the site be the
single source of truth for my content and share it with other services from there, which the
IndieWeb calls [POSSE](https://indieweb.org/POSSE) ([colophon](https://www.lqdev.me/colophon/)).
Back in 2018 I built it with Hexo and the Cactus theme
([Blogging tools](https://www.lqdev.me/posts/blog-tools/)). Today it runs on a static site
generator I wrote myself in F#.

The six principles below started as the "soul constraints" in the
[2026 architecture assessment](../archive/docs/architecture-assessment-2026.md), the rules any
change to the site has to respect. Each one links the post where I said it in my own words. The
decisions that follow from them are in the [architecture decision records](adr/README.md).

## Start as a website, add social features as components

The web is already social because people are on it, so I don't think a site has to be rebuilt as a
social app to take part. My site worked as a publishing platform first, and people could follow it
with RSS. When I added ActivityPub, the same posts picked up a social vocabulary and I didn't have
to abandon the site to get there
([Thoughts on the Social Web from FOSDEM 2026](https://www.lqdev.me/posts/fosdem-2026-social-web-thoughts/)).
I also ran my own Mastodon instance, and it went down every few weeks when the disk filled up. I
still want to be on the Fediverse, but I don't want to keep paying that maintenance tax.

In the code: the site is static files. Federation is JSON that the build writes into `_public/`,
plus 10 Azure Functions in `api/` for the parts that need a server, such as the inbox and WebFinger.

## One identity, many post types

I post different kinds of content that line up with
[IndieWeb post types](https://indieweb.org/posts#Types_of_Posts): articles, notes, responses,
bookmarks, media, and RSVPs. They all live under one identity, and that is what decouples
publishing from consumption. I choose where I publish (my site), others choose how they consume it
(their client), and the protocols handle the translation. I want to keep adding post types. In 2025
I named reviews and RSVPs as the next ones to design
([IndieWeb Create Day, July 2025](https://www.lqdev.me/posts/indieweb-create-day-2025-07/)), and
both exist now.

That makes the cost of adding a post type the number that matters most, and right now it is high.
Adding the marketplace type touched 26 files (`git show --stat 8192fd5b`). That is the number to
bring down.

In the code: `ContentTypes.fs` defines the 14 content types, and it is the one place their names
live.

## Own the whole stack

I'm a little extreme about owning the whole stack. I wrote my own static site generator, my own
Webmention service, and my own ActivityPub support (the FOSDEM post above). There are good services
that receive Webmentions for you, but I rolled my own for fun and to learn something new
([Accept Webmentions using F#, Azure Functions, and RSS](https://www.lqdev.me/posts/receive-webmentions-fsharp-az-functions-fsadvent/)).
The same goes for links: my domain is the source of truth, and it redirects to my profiles on other
platforms ([Site redirects in static websites](https://www.lqdev.me/posts/static-website-redirects/)).

In the code: `PersonalSite.fsproj` has 14 package references, and the Webmention one,
lqdev.WebmentionFs, is my own library.

## Keep it light

I don't track visitors. I used to self-host GoatCounter, but I don't really care about traffic, so
I got rid of it. What I care about is my output and what I'm publishing
([Computing website metrics using GitHub Actions](https://www.lqdev.me/posts/website-metrics-github-actions/)).
I like the slow and quiet web
([Down the Slow / Quiet Web Rabbit Hole](https://www.lqdev.me/posts/down-the-slow-web-rabbit-hole/)),
so the site tries to stay small. The text-only version aims for pages under 50 KB, and anything
that adds weight in the browser needs a good reason.

In the code: `TextOnlyBuilder.fs` builds the text-only site under /text/, and
`Scripts/check-site-sizes.sh` reports the site's size and file count on every deploy. In a build on
2026-11-23, 8 of the 3,730 text-only pages were over 50 KB.

## A person can maintain it without AI

If AI can no longer make changes, a competent F# developer should still be able to read the
structure without doing archaeology. So explicit beats implicit, and one copy of something beats
eleven. This one comes from the assessment, where I gave it as a requirement for the refactor.

In the code: the 11 decision records in `docs/adr/` say why the big pieces are the way they are.

## Don't let perfect be the enemy of good

When I get stuck on how something should fit into the site, I build the simpler piece first. How I
integrate it is a future me problem, and I'd rather work from what could be than from what already
exists (the Create Day post above). So changes here are small and easy to undo, and bigger ideas get
written up with their costs before anything is built.

In the code: federation features ship behind flags and get switched on one at a time.
[ADR 0009](adr/0009-at-protocol-integration.md) is still Proposed, and in `AtProtoBuilder.fs` the
flags for galleries, videos, and reposts are still off.
