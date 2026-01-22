---
post_type: "article" 
title: "Nostr - First Impressions"
description: "Some initial thoughts after connecting to the Nostr network using the Amethyst Android client"
published_date: "2026-01-22 16:31 -05:00"
tags: ["nostr","socialweb","activitypub","indieweb","fdroid","amethyst","openweb,"social","fediverse","protocol"]
---

I just wrapped up the MVP to [connect my website to the Fediverse by implementing a small subset of ActivityPub](/notes/website-now-natively-posts-to-the-fediverse-2026-01-22/). [Nostr](https://nostr.com/) is a protocol I've heard about but hadn't dipped my toes into, until today. I downloaded the [Amethyst client from F-Droid](https://amethyst.social/). 

The following are my off-the-cuff initial impressions of Nostr.

## The Good

- Creating an account was easy. I just provided a username and was immediately taken to the feed.
- The protocol seems simple. It's "just keys and events". In some ways, this simplifies account creation and enables building simple relays that are event / task-specific (search, outbox, DM).  
- Amethyst is preconfigured with a set of default relays so it solves the empty feed problem. You just create an account, pull up the global feed, and immediately start receiving posts.
- Live video? Given Nostr stands for (Notes and Other Stuff Transmitted by Relays), live video is covered under other stuff. Still I was pleasantly surprised to see it. 

## Not Ideal?

- The global feed is chaos. It's like drinking from a fire hose. There's so many posts coming in all at once it's hard to keep track.
- Tons of DeFi content. From what I've read, there's a large DeFi community presence on Nostr. Personally, I don't care for DeFi and Bitcoin content. Building a curated feed over time though should address this issue and reduce the amount of this content I see on my personal feed.

Overall, I like what I'm seeing and curating my feed is relatively simple to do so I'm not constantly bombarded with posts from the global feed. 

😔just like with ActivityPub and the Fediverse, I already know I'm going to end up running my own relay and start cross-posting content from my site to Nostr. It's just a matter of time. First I want to get my ActivityPub implementation in a more stable state where it manages itself and I can feel comfortable directing folks to subscribe to my Fediverse content through my website instead of the instance I'm currently running. Once that's done, I'll take the lessons and patterns from my ActivityPub implementation and build something similar for Nostr.