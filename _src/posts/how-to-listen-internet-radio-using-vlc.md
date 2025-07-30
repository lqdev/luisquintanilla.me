---
post_type: "article" 
title: "How to listen to internet radio using VLC"
description: "Use VLC to listen to internet radio streams"
published_date: "12/28/2021 16:24"
tags: [vlc, open source, radio]
---

## Introduction

Music streaming services like Spotify generally do a good job of making recommendations based on your music tastes. By applying algorithms to your listening history, they're able to provide you with a listening experience tailored to you. When it comes to music discovery though, it can be difficult to find new music so you often end up listening to music that sounds very similar. Internet radio can help expose you to music you otherwise wouldn't normally listen to. Radio stations like those on [SomaFM](https://somafm.com/) provide a wide variety of high quality music channels. There's other online radio stations out there, many which provide a built-in audio player. As I've written in previous posts though ([How to watch Twitch streams using VLC](/posts/how-to-watch-twitch-using-vlc.html) & [Podcast management with RSS & VLC](/notes/rss-vlc-podcast-management)), VLC is a versatile tool. As a result, it's no surprise you can also use it to listen to online radio streams. This post is mostly a note to myself, but I encourage you to try it as well. 

## Prerequisites

- [VLC](https://www.videolan.org/index.html)

The commands below were run on Linux. Since VLC is cross-platform, the same commands should work on those platforms as well. 

## Listen to internet radio

The first thing you'll need is the URL of the stream you want to listen to. Usually it's provided on the station's website. You can also browse through [RadioBrowser](https://www.radio-browser.info/) to find a station and its stream URL.

Once you have the URL, in the terminal, enter the following command:

```bash
vlc <STREAM-URL>
```

Replace `<STREAM-URL>` with the address of your stream.

After a few seconds, VLC launches and your stream should start playing.

If you prefer to stream audio without VLC's visual interface, use the following command.

```bash
cvlc <STREAM-URL>
```

Note that without the UI you won't have access to any playback controls. 

Happy listening! 
