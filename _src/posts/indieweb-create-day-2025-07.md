---
post_type: "article" 
title: "IndieWeb Create Day - July 2025"
description: "A recap of my first IndieWeb Create Day"
published_date: "2025-07-06 20:09"
tags: ["indieweb","iwc","community","website"]
---

Since it was a holiday weekend in the U.S. that kind of snuck up on me, I found myself with nothing planned on a Saturday. So I chose to spend it creating stuff for my website with the IndieWeb community during [IndieWeb Create Day](https://events.indieweb.org/2025/07/indieweb-create-day-3q2PTCbGioi9).

Over the last few months I've been overthinking my website redesign and while I've made several attempts at it, I've never been satisfied the outcome, so I throw away all the progress I've made and go back to the drawing board. 

Yesterday, I decided to not let perfect be the enemy of good and the approach I took was creating just a simpler piece of functionality outside of my website. How I integrate it into my website is a future me problem. But I want to work from a place of creativity and complete freedom to think of what could be rather than what is. 

With that in mind, I set out to sketch out how I want to create and render media (image, audio, video) posts. The approach I took used a combination of front-matter YAML and custom markdown media extensions. The front-matter YAML is something that I already use for my website and it's something that I want to continue using. However, in contrast to my current website, I like that the front-matter was kept simple and only includes a basic amount of information. The actual post content was handled by my custom markdown extension which leveraged YAML-like syntax to define media content. What's great about this is that it is composable so once I got one type of media working, the rest for the most part "just worked". I could even mix different media types within the same post with no additional work or code changes required. Once I had the skeleton, it was all about refactoring, documentation, adding finishing touches, and vibe-coding some CSS which Cluade did a relatively good job with given the aesthetic I was going for. 

Overall, I'm happy with the end result. 

![A screenshot of a website post containing an image and audio player](/images/feed/indieweb-create-day-2025-07.png)

For more details, you can check out the [repo](https://github.com/lqdev/indieweb-create-day-2025-07).

At some point, I want to be able to integrate these media posts into my static site generator but for the time being, there are other kinds of posts such as reviews, RSVPs, and other [post types](https://indieweb.org/posts#Types_of_Posts) that I want to design and eventually also support on my website. I liked the approach I took this time around because it gave me the freedom to explore posibilities rather than constrain my creativity to what I've already built. So I think I'll keep doing the same for subsequent post types. 

At the end of the day, it was nice seeing everyone else's projects. My favorite one was Cy's recipe website. I want to be like them when I grow up 🙂.