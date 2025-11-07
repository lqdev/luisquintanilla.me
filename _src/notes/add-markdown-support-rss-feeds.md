---
post_type: "note"
title: "Added markdown support to RSS feeds"
published_date: "11/7/2025 1:22 PM -05:00"
tags: ["plaintext","markdown","rss","microblog","openweb","indieweb"]
---

I just learned about this [proposal to add markdown to RSS feeds](https://source.scripting.com/#1653758422000), which [Manton](https://www.manton.org/) [implemented in Micro.blog](http://scripting.com/2025/11/07.html#a152336). 

This is such a neat idea. Since I author my blog posts in Markdown, exposing it via RSS was relatively trivial because a lot of the plumbing was already there. 

This is the [PR](https://github.com/lqdev/luisquintanilla.me/pull/786) where I had GitHub Copilot Coding Agent implement the feature. 

Here's a snippet from my [main feed](/all.rss):

```xml
<item>
<title>Dynamic OPML for Pocket Casts</title>
<description><![CDATA[[reply] <blockquote class="blockquote"> <p>How could this work? A new feature for <a href="https://opml.org/spec2.opml#subscriptionLists">OPML subscription lists</a>. Today it's used as the import/export format for lists. But that's a one-time thing. Instead I want to give Pocket Casts the URL of an OPML file with my podcast subscriptions from the desktop.<a href="http://scripting.com/2025/11/06/141023.html#a143319">#</a></p> </blockquote> <p>This is exactly the thinking behind my <a href="https://www.lqdev.me/podroll">podroll</a> and other <a href="https://www.lqdev.me/collections">collections</a> on my site that I provide an OPML file for. I want a single source of truth for my subscriptions that I can share with others. Sadly it's not dynamic because I still have to manually update the OPML file and re-import into my podcasting client. Having read / write capabilities from the client so that whenever I subscribe to a podcast, the OPML file is updated would make the experience even better.</p> ]]></description>
<link>https://www.lqdev.me/responses/podroll-dynamic-opml</link>
<guid>https://www.lqdev.me/responses/podroll-dynamic-opml</guid>
<pubDate>2025-11-06 12:39 -05:00</pubDate>
<category>opml</category>
<category>rss</category>
<category>podcasts</category>
<category>podroll</category>
<category>automattic</category>
<category>pocketcasts</category>
<source:markdown>
<![CDATA[ --- title: "Dynamic OPML for Pocket Casts" targeturl: http://scripting.com/2025/11/06/141023.html?title=dynamicOpmlForPocketCasts response_type: reply dt_published: "2025-11-06 12:39 -05:00" dt_updated: "2025-11-06 12:39 -05:00" tags: ["opml","rss","podcasts","podroll","automattic","pocketcasts"] --- > How could this work? A new feature for [OPML subscription lists](https://opml.org/spec2.opml#subscriptionLists). Today it's used as the import/export format for lists. But that's a one-time thing. Instead I want to give Pocket Casts the URL of an OPML file with my podcast subscriptions from the desktop.[#](http://scripting.com/2025/11/06/141023.html#a143319) This is exactly the thinking behind my [podroll](/podroll) and other [collections](/collections) on my site that I provide an OPML file for. I want a single source of truth for my subscriptions that I can share with others. Sadly it's not dynamic because I still have to manually update the OPML file and re-import into my podcasting client. Having read / write capabilities from the client so that whenever I subscribe to a podcast, the OPML file is updated would make the experience even better. ]]>
</source:markdown>
</item>
```

I don't have Mac or iOS so I can't test with NetNewsWire, so if anyone would be kind of enough to validate whether this works for them and [send me a message](/contact), I'd greatly appreciate it.