---
post_type: "article" 
title: "HTTP Signature Verification and Migration Planning"
description: "Latest status update from my ActivityPub implementation"
published_date: "2026-01-23 21:58 -05:00"
tags: ["mastodon","fediverse","indieweb","activitypub","azure"]
---

More progress on the AP implementation on my site. I got HTTP signature verification on Follow activities working and started the groundwork to migrate from my current instance to my website. 

Some things I want to do before migrating:

1. Export my archive, I'm especially interested in preserving who I'm follows and bookmarks.
    1. For bookmarks: Create a page on my site that links to the bookmarks.
    1. For follows: Create an OPML file linking to their RSS feeds and add those feeds to my feed reader.

One thing I will miss after I retire my instance is the timeline. I always discover interesting posts and people that way. My plan is to follow RSS feeds for tags that I'm interested in. I know it's not as good or spontaneous as the timeline but it's a way to stay engaged in the conversation.

Also, my site effectively will work one-way for now. I don't have a way to receive replies or DMs, nor do I have a way of replying to people directly from my site. Maybe that's something I'll add later on but not a priority at the moment. 

My main priority at the moment is to maintain a presence in the Fediverse without having to maintain my own instance. I know technically I could just join someone else's instance, but I don't want to create yet another account nor become a maintenance burden for someone else. 

More importantly though, I want my website to be my digital hub, with protocols like ActivityPub, Nostr, and AT Protocol serving as spokes to reach different networks. My content and identity remain on my site, independent of any single platform.  If any protocol or network disappears, my content and identity remain intact.