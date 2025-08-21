---
title: "StreamBuilder: our open-source framework for powering your dashboard"
targeturl: https://engineering.tumblr.com/post/722102563011493888/streambuilder-our-open-source-framework-for
response_type: reshare
dt_published: "2024-12-17 19:03 -05:00"
dt_updated: "2024-12-17 19:03 -05:00"
tags: ["tumblr","algorithms","feed","socialmedia"]
---

Interesting post. Whether building your feeds or plugging into existing platforms like [Bluesky](https://bsky.social/about/blog/3-30-2023-algorithmic-choice), this framework could serve as a good starting point. 

> Today, we’re abnormally jazzed to announce that we’re open-sourcing the custom framework we built to power your dashboard on Tumblr. We call it StreamBuilder, and we’ve been using it for many years.

> StreamBuilder has a lot going on. The primary architecture centers around “streams” of content: whether posts from a blog, a list of blogs you’re following, posts using a specific tag, or posts relating to a search. These are separate kinds of streams, which can be mixed together, filtered based on certain criteria, ranked for relevancy or engagement likelihood, and more.

> So, what’s included in the box?  
> <br>
> - The full framework library of code that we use today, on Tumblr, to power almost every feed of content you see on the platform.  
> - A YAML syntax for composing streams of content, and how to filter, inject, and rank them.  
> - Abstractions for programmatically composing, filtering, ranking, injecting, and debugging streams.  
> - Abstractions for composing streams together—such as with carousels, for streams-within-streams.  
> - An abstraction for cursor-based pagination for complex stream templates.  
> - Unit tests covering the public interface for the library and most of the underlying code.  

[GitHub Repo](https://github.com/Automattic/stream-builder)