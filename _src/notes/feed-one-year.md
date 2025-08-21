---
post_type: "note" 
title: "Personal Feed - One Year In"
published_date: "09/29/2022 10:55 -05:00"
tags: ["microblog","blog","feed","rss","indieweb","openweb","internet"]
---

It's been a year since I created this feed and [started posting](/notes/hello-world) mainly on my website. Since then, I've:

- Used a [shorter domain](/notes/lqdevme-redirect) to redirect to my website domain.
- Created [VS Code snippets](/posts/automate-yaml-front-matter-vs-code-snippets/) to simplify metadata tagging.
- Used [github.dev](/notes/surface-duo-blogging-github-dev) as the main interface for authoring and editing posts. For longer posts, I use VS Code locally but for these smaller microblog-style posts, github.dev makes it really easy.
- Syndicated posts to Twitter and [Mastodon](/notes/mastodon-posse-enabled).
- Created a [response feed](/feed/responses) for interactions like replies, reshares (repost), and likes (favorites) with support for sending [Webmentions](/notes/webmentions-partially-implemented/). 
- Created [RSS feeds](/feed) for each of my feeds.

It hasn't happened overnight. Instead, it's been the result of small incremental efforts over time.

Overall, I don't think I've posted any more or less than I do on other platforms. 

What I have enjoyed the most though has been:

- Learning.
- Building the tools and processes to author and share content.
- Owning my content. My website acts as the single source of truth. 
- Not requiring others to have or create accounts on individual platforms to access content.
- Choosing how I author content. I'm sure this post is way over the 280 character limit and if there's a typo, I can just edit the post and republish the website :slightly_smiling_face:

Going forward, I plan to: 

- Update RSS feeds to include post content, not just link to the post.
- Accept Webmentions. I'm about halfway done with the main parts of my [implementation](https://github.com/lqdev/luisquintanilla.me/blob/main/_scratch/receive-web-mentions.md).  
- Implement tags for easier discoverability / search. 
- Consolidate metadata for posts (articles, microblogs, responses).