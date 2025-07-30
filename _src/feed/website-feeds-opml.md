---
post_type: "note"
title: "OPML for website feeds"
published_date: 2024-08-01 20:08 -05:00
tags: ["rss","opml","indieweb","feeds","internet","protocols","webstandards","standards","web"]
---

While thiking about implementing [`.well-known` for RSS feeds on my site](/responses/well-known-feeds/), I had another idea. Since that uses OPML anyways, I remembered recently [doing something similar for my blogroll](/notes/blogroll-discovery-implemented//).

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
		<outline title="Blog" text="Blog" type="rss" htmlUrl="/posts/1" xmlUrl="/blog.rss" />
		<outline title="Microblog" text="Microblog" type="rss" htmlUrl="/feed" xmlUrl="/microblog.rss" />
		<outline title="Responses" text="Responses" type="rss" htmlUrl="/feed/responses" xmlUrl="/responses.rss" />
		<outline title="Mastodon" text="Mastodon" type="rss" htmlUrl="/mastodon" xmlUrl="/mastodon.rss" />
		<outline title="Bluesky" text="Bluesky" type="rss" htmlUrl="/bluesky" xmlUrl="/bluesky.rss" />
		<outline title="YouTube" text="YouTube" type="rss" htmlUrl="/youtube" xmlUrl="/bluesky.rss" />
	  </body>
	</opml>
	```

1. Add a `link` tag to the `head` element of my website.

	```html
	<link rel="feeds" type="text/xml" title="Luis Quintanilla's Feeds" href="/feed/index.opml">
	```
