---
post_type: "wiki" 
title: "Owncast"
last_updated_date: "01/30/2025 20:19"
tags: owncast,fediverse,livestream
---

## Overview

Owncast is a free and open source live video and web chat server for use with existing popular broadcasting software.

## Simulcast

By default, when you set up streaming software, it will only stream to your Owncast instance. If you want to simultaneously broadcast to various services, you'll have to either use something like Restream or you can also use FFMPEG.

### YouTube

With your brodcast stream started, use the following FFMPEG command to simulcast to YouTube. 

```bash
ffmpeg -v verbose -re -i https://YOUR-OWNCAST-SERVER/hls/stream.m3u8 -c:v libx264 -c:a aac -f flv rtmp://a.rtmp.youtube.com/live2/YOUR-STREAM-KEY
```

This command will copy the video and audio feeds from your HLS Owncast live stream and forward them to YouTube.

## Ressources

- [Owncast](https://owncast.online/)
- [OBS Studio: Stream to multiple platforms or channels at once](https://obsproject.com/forum/resources/obs-studio-stream-to-multiple-platforms-or-channels-at-once.932/)