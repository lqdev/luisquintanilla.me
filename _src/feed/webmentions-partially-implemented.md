---
post_type: "note" 
title: "Webmentions (partially) implemented"
published_date: "09/17/2022 19:59"
tags: ["indieweb","webmentions","fsharp","website"]
---

Success! I just partially implemented [Webmentions](https://www.w3.org/TR/webmention/) for my website. Although I haven't figured out a good way to receive Webmentions yet, I'm able to send them. Fortunately most of the work was done, as detailed in the post [Sending Webmentions with F#](/posts/sending-webmentions-fsharp-fsadvent/). The rest was mainly a matter of adapting it to my static site generator. 

Below is an example of a [post on my website](/responses/webmention-test-1/) being displayed in the [Webmention test suite website](https://webmention.rocks/test/1).

**Source**

![Webmention on lqdev.me](https://user-images.githubusercontent.com/11130940/190879250-4554750f-b435-4627-bad9-ecc3d96f9ed0.png)

**Target**

![Webmention displayed in webmention test suite](https://user-images.githubusercontent.com/11130940/190879274-f6566225-2173-4213-a3d0-eeb9fdc67df9.png)

What does this mean? It means I can comment on any website I want, regardless of whether they allow comments or not. As if that weren't enough, I have full ownership of my content as my website is the single source of truth. As a bonus, if the website I comment on supports receiving Webmentions, my post will be displayed on their website / articles as a comment. The next step is to handle deleted comments, but so far I'm happy with the progress. 