---
post_type: "note" 
title: "First live stream using Owncast done"
published_date: "2025-01-20 18:33"
tags: ["owncast", "livestream", "selfhost", "opensource", "fediverse", "video", "azure"]
---

Earlier today I tested out my [self-hosted Owncast setup](/posts/deploy-owncast-azure/).

[![Video Thumbnail from live stream](http://img.youtube.com/vi/3opjC7fEAJs/0.jpg)](https://www.youtube.com/watch?v=3opjC7fEAJs "Video Thumbnail from live stream")

Overall, I think it went well considering I was using the built-in camera and mic on my laptop. Despite sharing my screen, using the browser, broadcasting and recording the stream, the laptop seemed to handle it just fine. I didn't get a chance to test writing and running code which is going to make up a bulk of the content on stream. 

The flow on OBS is straightforward and simple. I might automate it either using my Stream Deck or just mapping hotkeys. It's simple enough that I only need to transition between a few scenes. 

As far as hosting the recordings, the process of uploading to YouTube was straightforward. It took a little over 10 minutes, but that's something that I can just do in the background so not really worried about that. For the time being, I think I'll just use YouTube to host the videos and mark them as unlisted. No particular reason behind that other than I only want YouTube to serve as my video host. The main place I plan on showcasing and organizing videos is on my website. 

For the time being, I'll just create notes like these. 

Eventually though, I'd like to have a video post type which shows up on my feed. The post card will only display the video but the detailed view, I'd like for it to be like my presentation pages. Except, instead of a presentation, it'd contain the video, show notes, maybe the transcript, and links mentioned during the stream. Also, I'd like video posts to have their own RSS feed just like other posts on this page so folks who are interested in following along can via their RSS reader. 

A few other things I want to figure out are:

- Setting up a custom domain that points to my Owncast server rather than the default one provided by Azure Container Apps.
- Displaying and managing chat outside of the built-in Owncast frontend. 