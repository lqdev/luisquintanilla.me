---
post_type: "article"
title: "A Low-Friction Follow Workflow for My Website"
description: "Using GitHub Issues to add follows to my blogroll and podroll, publish them with OPML, and send follow Webmentions."
published_date: "2026-10-30 16:27 -05:00"
tags: ["github","indieweb","rss","opml","webmentions","workflow"]
---

In a [previous post](/posts/github-issue-publishing-workflow/), I described how I use GitHub issue forms and Actions as a lightweight way to publish from my phone while keeping the content in my site repository. I wanted to use that same approach for another small social interaction: following someone.

My website is the hub where I want my activity to live. Other networks and services are spokes. I wrote more about that framing in my [thoughts on the social web after FOSDEM](/posts/fosdem-2026-social-web-thoughts/).

DISCLAIMER: AI was used to help me organize and improve the flow of this post. Ideas and thoughts expressed are my own.

## What does it mean to follow someone on the web?

A blogroll is a public list of blogs and websites I follow. It gives people a way to discover sites I read and shows what I am interested in. If one of those sites publishes a feed, such as RSS, Atom, or JSON Feed, I can also subscribe to it in the reader I choose. A feed is a machine-readable list of updates that a reader checks for new content. I wrote more about why I like RSS in [Rediscovering the RSS protocol](/posts/rediscovering-rss-user-freedom/).

Those are two related but separate things: listing a site on my blogroll and subscribing to its feed. Neither one sends the site owner a notification. For that, the IndieWeb has the [`u-follow-of` microformat](https://indieweb.org/u-follow-of), which marks a link as a follow relationship. A [Webmention](https://indieweb.org/Webmention) is a standard way for one website to notify another that it has linked to it. If the other site accepts follow Webmentions, it can let the owner know or display the follow on their site.

OPML helps make a list of feeds portable. It is a format for organizing feed subscriptions that many readers can import or export. I publish [blogroll](/collections/blogroll/) and [podroll](/collections/podroll/) collections, along with [topic-based starter packs](/collections/starter-packs/), as browseable pages and OPML files. I wrote about using OPML and RSS for [starter packs](/posts/rss-opml-starter-packs/). These lists give me one place to show who I follow and make it easy to import a collection into a reader.

There is a limit, though. Importing OPML into a reader is a one-time action, not an ongoing sync. I may also subscribe to feeds in my reader that I do not want to list publicly. Keeping the reader synchronized is a separate problem, and I will leave that for another post.

## Making a follow less manual

The part I wanted to improve was maintaining the public lists. Adding someone meant editing the blogroll or podroll data by hand. And while the list showed who I followed, it did not notify them.

Since my publishing workflow already uses GitHub Issues as forms, I applied the same pattern to follows. The [follow issue template](https://github.com/lqdev/luisquintanilla.me/blob/main/.github/ISSUE_TEMPLATE/follow.yml) asks for a feed URL, site URL, title, and whether the entry belongs in the blogroll or podroll. A GitHub Action passes those details to an F# script. The script checks the feed and avoids duplicates, updates the matching data file, then opens a pull request for me to review.

When I merge that PR, the normal publishing workflow regenerates the collection pages and OPML files. The blogroll and podroll links are marked with `u-follow-of`, and the publish workflow checks for additions or removals and sends a Webmention from the collection page to the followed site. The data change still goes through a pull request, so I can check it before it is published.

The first example was [following Ellane W.](https://github.com/lqdev/luisquintanilla.me/pull/2944), which added her feed to the blogroll. The implementation is in [PR #2942](https://github.com/lqdev/luisquintanilla.me/pull/2942).

This solves the manual editing and notification parts of following. It does not keep my feed reader in sync. I also want to make starting a follow even quicker by having [GitHub Post PWA](https://github.com/lqdev/github-post-pwa) or the [RSS Browser Extension](https://github.com/lqdev/rss-browser-extension) open a prefilled issue. The form has a feed URL query parameter, but wiring up that one-click action is still a follow-up.

For now, this is a small, low-cost step that keeps the website as the hub. My list of follows stays on my site, the feeds are portable through OPML, and the people I follow can be notified without adding another always-on service.
