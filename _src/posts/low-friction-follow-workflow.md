---
post_type: "article"
title: "A Low-Friction Follow Workflow for My Website"
description: "Using GitHub Issues to add follows to my blogroll and podroll, publish them with OPML, and send follow Webmentions."
published_date: "2026-10-30 16:27 -05:00"
tags: ["github","indieweb","rss","opml","webmentions","workflow"]
---

In a [previous post](/posts/github-issue-publishing-workflow/), I described my GitHub Issues-based CMS and mobile publishing flow. I want to use that same foundation for follows.

On a social app, tapping Follow adds someone to my list, subscribes me to updates, and may notify them. I want that ease without handing the relationship to the app.

My website is my digital home and the hub for my identity, content, and activity. Social networks and protocols are spokes that help me reach people on other services; they extend the hub, not replace it. If one disappears, my identity and published work remain on my site. I wrote about this model in my [FOSDEM reflections](/posts/fosdem-2026-social-web-thoughts/). My public following list should live on the hub too.

DISCLAIMER: AI was used to help me organize and improve the flow of this post. Ideas and thoughts expressed are my own.

## Building a follow on the open web

A [blogroll](https://indieweb.org/blogroll) is a curated list of links to blogs and sites I follow. It is an old web practice, not unique to me. It helps people discover what I read; on its own, it neither subscribes me to updates nor tells the site owner.

For blogs that publish an open feed like RSS, I can subscribe with the reader I choose. That is separate from the public list: the blogroll helps with discovery, while the feed gives my reader updates. I wrote more about open feeds in [Rediscovering the RSS protocol](/posts/rediscovering-rss-user-freedom/).

To notify the owner, I can mark the blogroll link with the [`u-follow-of` microformat](https://indieweb.org/u-follow-of) and send a [Webmention](https://indieweb.org/Webmention). Webmention is a standard for notifying another site when I link to it. A site that accepts follow Webmentions can tell the owner or display the follow. That adds a notification to the blogroll relationship.

OPML makes feed lists portable: readers can import or export them. My [collections](/collections/) include a [blogroll](/collections/blogroll/), [podroll](/collections/podroll/), [YouTube channels](/collections/youtube/), [forums](/collections/forums/), and topic-based [starter packs](/collections/starter-packs/). Some combine different feeds. I wrote about using RSS and OPML for Bluesky and other starter packs in [this post](/posts/rss-opml-starter-packs/).

The follow workflow below only updates my blogroll or podroll. My OPML collections are the public source of truth for feeds I choose to share, not all my reader subscriptions. Importing OPML is a snapshot; later changes do not update existing copies in clients.

## Reusing what I already have

I want to improve both manual list maintenance and synchronization; here I focus on the first. Finding feeds can take more effort than editing a file. Blog and podcast feed URLs are not always easy to find, and without one I cannot add the site to these lists. I then copy the URL and site details into the form. The [RSS Browser Extension](https://github.com/lqdev/rss-browser-extension) detects feeds on pages, as I wrote in [my post about building it](/posts/taking-claude-code-web-for-a-spin/), but I still have to submit a follow issue.

I already use GitHub issues to create posts, and my site sends Webmentions for interactions like responses and RSVPs during publishing. I reused those pieces rather than add another service. The [follow issue template](https://github.com/lqdev/luisquintanilla.me/blob/main/.github/ISSUE_TEMPLATE/follow.yml) starts the process:

1. I enter the feed URL, site URL, and title, then choose blogroll or podroll.
2. A GitHub Action runs an F# script to validate the input and feed, avoid duplicates, update the matching data file, and open a PR for review.
3. After merge, the site build updates the collection and OPML. The entry gets `u-follow-of` markup, and the publish workflow detects added or removed follows and sends a Webmention from the collection page to the site.

[PR #2942](https://github.com/lqdev/luisquintanilla.me/pull/2942) added this workflow. [Following Ellane W.](https://github.com/lqdev/luisquintanilla.me/pull/2944) was a successful example: after merge, the publish workflow detected the new blogroll entry and sent a follow Webmention.

## What comes next

This improves how I add a follow to the public list and lets the site notify the person I am following. It is not one click yet: I still submit the GitHub issue myself. I want to connect feed discovery in the [RSS Browser Extension](https://github.com/lqdev/rss-browser-extension) to the issue flow in [GitHub Post PWA](https://github.com/lqdev/github-post-pwa), which I already use to start other posts. The follow form accepts a prefilled feed URL, but that integration is still on my list.

I am also exploring OPML over [WebSub](https://www.w3.org/TR/websub/) for synchronization. WebSub is a W3C standard that uses hubs to notify subscribers when content changes. I want compatible clients to learn when my OPML list changes instead of requiring a new import. That is still in progress and deserves its own post.

This builds on the investments I have already made in my mobile publishing flow. GitHub gives me a low-friction form and a review step; my site publishes the list and sends the notification. My website stays the hub, with open feeds and standards connecting it to the people and tools around it.
