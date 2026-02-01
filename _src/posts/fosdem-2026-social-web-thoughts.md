---
post_type: "article" 
title: "Thoughts on the Social Web from FOSDEM 2026"
description: "Reflections from FOSDEM's Social Web track, hosting, identity, and why the web is already social"
published_date: "2026-01-31 22:14 -05:00"
tags: ["fosdem2026","fosdem","socialweb","indieweb","activitypub","fediverse","decentralization","community"]
---

I had the opportunity to attend FOSDEM 2026 virtually, and I spent almost all of my time in the [Social Web](https://fosdem.org/2026/schedule/track/social-web/) track.

A few themes kept coming up across talks. Some were explicit, some were between the lines. Either way, they prompted a bunch of thoughts I wanted to capture.

DISCLAIMER: AI was used to help me organize and improve the flow of this post. Ideas and thoughts expressed are my own. 

## Hosting is hard

In [*Building a sustainable Italian Fediverse: overcoming technical, adoption and moderation challenges*](https://fosdem.org/2026/schedule/event/VKHGXT-building_a_sustainable_italian_fediverse_overcoming_technical_adoption_and_moder/), there was a moment (not the main focus of the talk) where hosting came up in a way that really stuck with me. I’m paraphrasing, so apologies if I misrepresent anything, but the gist was:

- Hosting Mastodon is hard, so we simplify with hosting services like Masto.Host
- Hosting PixelFed and PeerTube is easier thanks to appliances like YunoHost

Based on my own experience, that rings true, with some nuance.

Getting Mastodon running isn’t actually the hardest part. The self-hosting docs are good enough in my opinion, and that’s how I originally stood up my instance at [toot.lqdev.tech](https://toot.lqdev.tech/@lqdev). I even maintain guides for [cleanup](https://lqdev.me/resources/wiki/mastodon-server-cleanup/) and [upgrades](/resources/wiki/mastodon-server-upgrades/) that largely mirror the official Mastodon documentation and release notes.

The harder part is everything after provisioning.

Mastodon (especially with federation enabled) can be resource-intensive, and that cost shows up fast even on a single-user instance. If I’m not staying on top of maintenance, disk fills up. Every few weeks, my instance will go down because I’ve run out of storage. Add database migrations, which can be error-prone, and you end up with a setup that’s straightforward to launch but expensive to operate. You pay in money for a big enough server, and you pay in time for ongoing maintenace.

I still want to participate in the Fediverse, but I don’t want to keep paying the maintenance tax for Mastodon. That’s one of the reasons [I implemented ActivityPub on my static site](/notes/website-now-natively-posts-to-the-fediverse-2026-01-22/) instead.

On the PixelFed side, I did try to self-host it once, and I couldn’t get it working cleanly from scratch. Some of that is on me (I’m not familiar with PHP), but either way, YunoHost was a lifesaver. With YunoHost, I had PixelFed up and running quickly, and what that ecosystem provides is genuinely impressive.

That said, I also learned the “operations” lesson there too. During an upgrade, something went wrong with the database, it got corrupted, and I couldn’t restore from backup. I ultimately took the instance down. I’m willing to attribute that to user error, but it still reinforces the bigger point.

The promise of federation and decentralization is that you can stand up your own node for yourself, your family, a school, a company, a city, even a government. In practice, that’s still too hard for most people unless they use appliances like YunoHost or managed hosting like Masto.Host.

And yes, those options mean giving up some control. But even with that tradeoff, I’d argue it’s still better than centralized platforms.

As someone fairly technical and a little extreme about owning the whole stack (I implemented my own static site generator, Webmentions service, and now ActivityPub), I still find this hard. I can’t imagine how unapproachable it feels if you’re not technical. I just wish it were simpler and more cost-effective to run these services without needing either deep system administration knowledge or active ongoing maintenance.

## One identity, many post types

In the talk, [*How to level up the Fediverse*](https://fosdem.org/2026/schedule/event/HVJRNV-how_to_level_up_the_fediverse/), Christine and Jessica talked about ActivityPub implementations and touched on something that really resonated with me.

The idea (again, paraphrasing) was that splitting content types by app (video goes to PeerTube, images go to PixelFed, microblogging goes to Mastodon) might not be the right long-term model. Instead, they suggested something closer to one place to publish and follow people, with rich post types handled in one identity and one experience.

That immediately made me think about Tumblr.

When I first heard [Tumblr was planning to implement ActivityPub](https://techcrunch.com/2022/11/21/tumblr-to-add-support-for-activitypub-the-social-protocol-powering-mastodon-and-other-apps/), I was excited because Tumblr is already “that kind of app.” You can publish videos, photos, polls, longer posts, and everything in between, all in one place. There was also talk about [moving Tumblr to WordPress](https://techcrunch.com/2024/08/28/tumblr-to-move-its-half-a-billion-blogs-to-wordpress/), which (in theory) could make ActivityPub integration even more powerful. But as of now, [Tumblr’s ActivityPub work seems to be paused](https://techcrunch.com/2025/07/01/automattic-puts-tumblr-migration-to-wordpress-on-hold/).

The more I think about it, the more this model makes sense, especially because the most important part isn’t the “single app.” It’s the single identity.

You should have one account where your content originates. Then people can consume it from different experiences. Maybe that is a video-focused client, maybe it is an image-first view, maybe it is a Mastodon-like timeline. The key is that you do not need separate accounts everywhere.

That’s essentially how I think about my website.

My site is my digital home and my identity. I post different content types which align with [IndieWeb post types](https://indieweb.org/posts#Types_of_Posts):

- Articles
- Notes
- Responses (reposts, replies, likes)
- Bookmarks
- Media (photos and videos)
- RSVPs

People can follow via RSS. And more recently, I implemented my own ActivityPub support so my posts generate native ActivityPub activities. That means Mastodon and other clients can follow and interact with my site directly.

What I like about this is that it decouples publishing from consumption.

I choose where I publish (my site). Others choose how they consume (their client). The protocols handle the translation.

## The web is already social and decentralized

In Social Web conversations, sometimes the tone implies the "social web" is separate from "the web".

I don't really buy that.

The web is social because people are on it. People use it to learn, create, find community, do commerce, argue, collaborate, share memes, and everything else. The web is also decentralized by default. That's the baseline architecture.

Dave Winer recently wrote about software being ["of the web"](http://scripting.com/2025/11/24/141418.html). Software that's built to share data, accept input, produce output, and let users move their data. Not locked into silos.

This is why I'm so bullish on a different architectural approach: **start as a website, add social capabilities as components.**

People are already using WordPress, Ghost, and Micro.blog to build sites. With an ActivityPub plugin, your existing web presence becomes followable and interactive in the Fediverse. The site remains a site. It just gets socially interoperable.

Bridgy Fed reinforces this. It takes what already exists on the web and helps it participate in social protocols, without forcing you to rebuild as a native social app first.

That's also my own setup. My website worked as a publishing platform and people could follow via RSS. When I implemented ActivityPub, it became progressively enhanced. Same posts, new social vocabulary. I didn't have to abandon my site. I just made it speak the social language.

## Modular and extensible feels like the right direction

This is the architectural vision I took away from Bonfire: [Building Modular, Consentful, and Federated Social Networks](https://fosdem.org/2026/schedule/event/3QHALR-bonfire_building_modular_consentful_and_federated_social_networks/).

The "opt-in pieces" approach is about choosing which parts you want, evolving your experience based on what you enable. It echoes [small pieces loosely joined](http://scripting.com/2026/01/30/140150.html). It's a practical model for a federated future:

- Start with the basic web
- Add social capabilities as components
- Get progressively more powerful as you opt in

Your site still works normally. When you speak the lingua franca of protocols like ActivityPub, you can express social intent in a way other systems understand.

So it's not "the web vs the social web." It's the web, with richer native social vocabulary.

## Conclusion

This probably reads like I’m nitpicking, but I’m genuinely bullish on federated and decentralized networks. That’s why I’m still participating.

What stood out to me at FOSDEM this year is momentum. Last year, the Social Web track was a half day. This year, it expanded to a full day. That signals to me that there are a lot of smart, passionate people working across protocol design, UX, moderation, policy, community, activism, and implementation, trying to build real alternatives to entrenched silos.

And the plurality of implementations is a strength. It encourages exploration, competition, and innovation.

My hope is that the “end state” isn’t a separate social web you have to join. It’s a web that continues to work as expected, but gets progressively enhanced when you opt into interoperable social protocols.

Ultimately, there isn’t “the web” and “the social web.” There's just the web, and social vocabularies that participants can adopt without thinking about it.
