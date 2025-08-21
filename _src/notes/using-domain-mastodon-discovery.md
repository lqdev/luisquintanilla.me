---
post_type: "note" 
title: "Using my domain for discovery in Mastodon"
published_date: "2024-11-19 09:35 -05:00"
tags: ["mastodon","fediverse","indieweb","openweb","web","smallweb","cdn","internet"]
---

For a while, I've known that I can use my domain and the WebFinger protocol to aid with discovery in the Fediverse, specifically Mastodon. 

I tried implementing this feature on my website a while back but was unsuccessful. 

Last night, I got an itch to try it again and got it working.

Basically, what I was doing wrong the first time was, I was trying to use `lqdev.me` as the domain. Currently, my domain is not set up to use apex / naked domains. Therefore, it makes sense why that didn't work. 

Once I used `www.lqdev.me`, everything seemed to be working fine. The implementation isn't perfect, because technically you can use any username. [Scott Hanselman does a nice job explaining that](https://www.hanselman.com/blog/use-your-own-user-domain-for-mastodon-discoverability-with-the-webfinger-protocol-without-hosting-a-server). The important part though is that [my persence on the Fediverse is now directly linked to my domain, just like it is with Bluesky](/notes/now-on-bluesky). This goes beyond the link verification Mastodon provides. As a result, it provides more flexibility and opportunities to shape my presence on the Fediverse.  

If you're interested in implementing it for yourself, there's a ton of guides, but here's the one I used: [Use your own user @ domain for Mastodon discoverability](https://guide.toot.as/guide/use-your-own-domain/).

I don't like the `www` subdomain. So far I've put up with it for a while but this might be the forcing function to finally getting the apex domain setup. 

In the meantime, you can search my account in Mastodon using `@lqdev@www.lqdev.me`.