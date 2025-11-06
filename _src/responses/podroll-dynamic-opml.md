---
title: "Dynamic OPML for Pocket Casts"
targeturl: http://scripting.com/2025/11/06/141023.html?title=dynamicOpmlForPocketCasts
response_type: reply
dt_published: "2025-11-06 12:39 -05:00"
dt_updated: "2025-11-06 12:39 -05:00"
tags: ["opml","rss","podcasts","podroll","automattic","pocketcasts"]
---

> How could this work? A new feature for [OPML subscription lists](https://opml.org/spec2.opml#subscriptionLists). Today it's used as the import/export format for lists. But that's a one-time thing. Instead I want to give Pocket Casts the URL of an OPML file with my podcast subscriptions from the desktop.[#](http://scripting.com/2025/11/06/141023.html#a143319)

This is exactly the thinking behind my [podroll](/podroll) and other [collections](/collections) on my site that I provide an OPML file for. I want a single source of truth for my subscriptions that I can share with others. Sadly it's not dynamic because I still have to manually update the OPML file and re-import into my podcasting client. Having read / write capabilities from the client so that whenever I subscribe to a podcast, the OPML file is updated would make the experience even better.