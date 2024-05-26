---
post_type: "note" 
title: "Blogroll discovery enabled"
published_date: "2024-05-25 22:44"
tags: ["blogroll","opml","rss","indieweb","openweb","socialweb","personalweb"]
---

Just enabled discovery for my [blogroll](/feed/blogroll) based on the [OPML blogroll spec](https://opml.org/blogroll.opml). While technically not part of the spec, I also have similar collections for [podcasts](/feed/podroll) and [YouTube](/feed/youtube). I've added those as well.

```html
<head>
    <!--...-->
    <link rel="blogroll" type="text/xml" title="Luis Quintanilla's Blogroll" href="/feed/blogroll/index.opml">
    <link rel="youtuberoll" type="text/xml" title="Luis Quintanilla's YouTube Roll" href="/feed/youtube/index.opml">
    <link rel="podroll" type="text/xml" title="Luis Quintanilla's Podroll" href="/feed/podroll/index.opml">
    <!--...-->
</head>
```

youtuberoll doesn't really roll off the tongue, but I'm hesitant to call it videoroll or vidroll. Ideas welcome!