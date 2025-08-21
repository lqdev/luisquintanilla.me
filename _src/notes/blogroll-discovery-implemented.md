---
post_type: "note" 
title: "Blogroll discovery enabled"
published_date: "2024-05-25 22:44"
tags: ["blogroll","opml","rss","indieweb","openweb","socialweb","personalweb"]
---

Just enabled discovery for my [blogroll](/collections/blogroll/) based on the [OPML blogroll spec](https://opml.org/blogroll.opml). While technically not part of the spec, I also have similar collections for [podcasts](/collections/podroll/) and [YouTube](/collections/youtube/). I've added those as well.

```html
<head>
    <!--...-->
    <link rel="blogroll" type="text/xml" title="Luis Quintanilla's Blogroll" href="/collections/blogroll//index.opml">
    <link rel="youtuberoll" type="text/xml" title="Luis Quintanilla's YouTube Roll" href="/collections/youtube//index.opml">
    <link rel="podroll" type="text/xml" title="Luis Quintanilla's Podroll" href="/collections/podroll//index.opml">
    <!--...-->
</head>
```

youtuberoll doesn't really roll off the tongue, but I'm hesitant to call it videoroll or vidroll. My concerns mainly come from the fact that YouTube != video. 

Ideas welcome!