---
post_type: "note" 
title: "Now accepting webmentions"
published_date: "2022-12-18 22:41 -05:00"
tags: ["indieweb","webmentions","community","fsharp","azurefunctions","azure"]
---

It took me a while to get all the pieces working but I'm excited that my website now accepts [webmentions](https://www.w3.org/TR/webmention/)!

A few months ago I implemented [sending webmentions](/notes/webmentions-partially-implemented/).

Accepting them was a bit tricky because there were a few design decisions I was optimizing for.

In the meantime, you can check out [WebmentionFs](https://github.com/lqdev/WebmentionFs), an F# library I built for validating and receiving webmentions along with the [WebmentionService](https://github.com/lqdev/WebmentionService) Azure Functions backend that I'm using to process webmentions for my website.   

Also, feel free to send me a webmention. Here's my endpoint for now [https://lqdevwebmentions.azurewebsites.net/api/inbox](https://lqdevwebmentions.azurewebsites.net/api/inbox). Check out this [blog post](/posts/sending-webmentions-fsharp-fsadvent/) where I show how you can send webmentions using F#. You can use any HTTP Client of your choosing to send an HTTP POST request to that endpoint with the body containing a link to one of my articles (target) and the link to your post (source). 

Stay tuned for a more detailed post coming on December 21st!