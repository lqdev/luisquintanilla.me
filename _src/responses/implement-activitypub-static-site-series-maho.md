---
title: "Implement ActivityPub on a static site series by maho.dev"
targeturl: https://maho.dev/2024/02/a-guide-to-implement-activitypub-in-a-static-site-or-any-website/
response_type: reshare
dt_published: "2024-11-19 10:03"
dt_updated: "2024-11-19 10:03 -05:00"
tags: ["acvititypub","fediverse","staticsite","blogging","indieweb","openweb","personalweb","dotnet","internet"]
---

I've been following along with this amazing series from [Maho](https://maho.dev/) on implementing ActivityPub on static websites.  

As I think about what I want out of my website and the way I engage in the Fediverse, there's a lot of overlap. 

Today, what I mainly use my Mastodon instance for is:

1. Having a Fediverse presence on a self-hosted Mastodon instance. 
2. [Cross-posting posts on by website using the POSSE pattern](/posts/rss-to-mastodon-posse-azure-logic-apps/).

Although I have learned a ton from self-hosting my own Mastodon instance, neither of the points listed above require me self-hosting or even having an account on someone else's instance. 

Yesterday, [I took the first step in linking my Fediverse presence to my domain](/feed/using-domain-mastodon-discovery/). That fulfills my first requirement. 

I rarely post original content on Mastodon and for consumption, [I already subscribe to accounts and tags via RSS](/feed/subscribed-to-1042-feeds-newsblur). Therefore, the second requirement is one that can naturally occur without having to use a Mastodon instance as an intermediary. I can just post on my website and because my posts can show up in my outbox, there's no need to POSSE. I don't want my presence to be limited to Mastodon though, but since I plan on supporting media, reviews, and other types of posts, I expect my posts to be accessible across other platforms on the Fediverse like Pixelfed, Bookwyrm, and many others. 

Since my website and website features are built using .NET, Maho's guide simplifies my implementation. Because [I'm not using any of the existing static site generators and rolled my own](/colophon), that means that there's going to be some effort and customizations required on my end. It would no different from my [custom Webmentions implementation](/posts/receive-webmentions-fsharp-az-functions-fsadvent/) though. My hope is that I will save time I often spend maintaining the server as well as money since I no longer need to rent a server to host my instance. At the same time, I get to learn and contribute to building a more open, decentralized, and personal web.