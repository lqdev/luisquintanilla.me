---
post_type: "note" 
title: "Use AI to generate a blogroll others can subscribe to"
published_date: "2024-03-21 22:01"
tags: ["blogroll","indieweb","ai","copilot","rss","openweb"]
---

I've seen several posts about blogrolls recently across my feed. My own needs some updating, but you can find it [here](/collections/blogroll/). I also [created one for podcasts](/collections/podroll/) which I call a podroll. 

I've also seen posts about making the indie web easier. 

With both of those in mind, that's how this post came to be.

Let's say that you have a blogroll on your website which might just be a list of links to the respective RSS feeds. 

If someone wanted to subscribe to the feeds on your blogroll, they could just copy the links and add them to their RSS reader. However, as your blogroll grows, doing this in bulk can be tedious. 

One way you can make it easier is using OPML files. Many RSS readers support importing feeds using OPML files. 

You could go through the process of figuring out what OPML is, how you need to format the file, and then do the work of creating and populating the file. Or...you could use AI. 

![Copilot chat being asked to generate OPML file](https://github.com/lqdev/luisquintanilla.me/assets/11130940/2a24fd44-0ae4-46a1-bd55-68988927a33e)

In this example, I opened up Copilot, provided it a list of links to RSS feeds and asked it to generate an OPML file. You can take the outputs generated using AI, save them to a file, and add it to your website. 

Now people who want to subscribe to the feeds on your blogroll can just load the OPML file into their RSS reader and all the feeds will be automatically added.
