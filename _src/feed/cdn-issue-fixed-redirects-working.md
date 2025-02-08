---
post_type: "note" 
title: "Redirect URLs working again"
published_date: "2025-02-08 14:38 -05:00"
tags: ["cdn","azure","rss","ownyourlinks","cdn","frontdoor","community"]
---

Thanks to the kind e-mail from [benji.dog](https://www.benji.dog/), I realized people couldn't subscribe to my RSS feeds using the links provided in the [/subscribe](/subscribe) page.

I'd noticed something weird had been going on for the past few days but since the rest of my site was working, I didn't pay attention to it.  

When I went in earlier today to fix the issue, I realized because of changes to [Azure CDN](https://learn.microsoft.com/azure/cdn/edgio-retirement-faq), none of my redirects were working.

My site kept working fine, but that's because the migration was autmatically done by Azure. 

What wasn't automatically migrated were my CDN rules which I'd set up to [simplify URLs](/feed/new-rss-feed-links) and [own my links](/feed/own-your-rss-links). 