---
post_type: "note" 
title: "Just added Fediverse Open Graph meta tag to my website"
published_date: "2024-07-02 21:41 -05:00"
tags: ["mastodon","fediverse","blogging","indieweb","blogs","news","journalism","openweb","distributedweb","smallweb","personalweb","opengraph","html"]
---

I learned from [Matthias Ott](https://matthiasott.com/notes/highlighting-blogging-on-mastodon) that Mastodon created a new Open Graph meta tag which displays a direct link to the website owner's Fediverse (Mastodon, Pixelfed, Threads, etc...) profile as part of the URL preview card on the Mastodon web and mobile apps. 

I just added support for it on my website. 

All I had to do was add the following tag to my site.

```html
<meta property="fediverse:creator" content="@lqdev@toot.lqdev.tech">
```

I'm not running the nightly version of Mastodon on my instance, but if anyone on an instance where this is already supported like mastodon.social can verify and let me know it's working, it's much appreciated.

