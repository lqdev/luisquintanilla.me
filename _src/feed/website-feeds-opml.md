---
post_type: "note"
title: "OPML for website feeds"
published_date: 2024-08-01 20:08 -05:00
tags: ["rss","opml","indieweb","feeds","internet","protocols","webstandards","standards","web"]
---

While thiking about implementing [`.well-known` for RSS feeds on my site](/feed/well-known-feeds/), I had another idea. Since that uses OPML anyways, I remembered recently [doing something similar for my blogroll](/feed/blogroll-discovery-implemented/).

The concept is the same, except instead of making my blogroll discoverable, I'm doing it for my feeds. At the end of the day, a blogroll is a collection of feeds, so it should just work for my own feeds. 

The implementation ended up being:

1. Create an OPML file for[ each of the feeds on by website](/subscribe).

	```xml
	<opml version="2.0">
	  <head>
		<title>Luis Quintanilla Feeds</title>
		<ownerId>https://www.luisquintanilla.me</ownerId>
	  </head>
	  <body>
		<outline title="Blog" text="Blog" type="rss" htmlUrl="https://www.lqdev.me/posts/1" xmlUrl="https://www.lqdev.me/blog.rss" />
		<outline title="Microblog" text="Microblog" type="rss" htmlUrl="https://www.lqdev.me/feed" xmlUrl="https://www.lqdev.me/microblog.rss" />
		<outline title="Responses" text="Responses" type="rss" htmlUrl="https://www.lqdev.me/feed/responses" xmlUrl="https://www.lqdev.me/responses.rss" />
		<outline title="Mastodon" text="Mastodon" type="rss" htmlUrl="https://www.lqdev.me/mastodon" xmlUrl="https://www.lqdev.me/mastodon.rss" />
		<outline title="Bluesky" text="Bluesky" type="rss" htmlUrl="https://www.lqdev.me/bluesky" xmlUrl="https://www.lqdev.me/bluesky.rss" />
		<outline title="YouTube" text="YouTube" type="rss" htmlUrl="https://www.lqdev.me/youtube" xmlUrl="https://www.lqdev.me/bluesky.rss" />
	  </body>
	</opml>
	```

1. Add a `link` tag to the `head` element of my website.

	```html
	<link rel="feeds" type="text/xml" title="Luis Quintanilla's Feeds" href="/feed/index.opml">
	```
