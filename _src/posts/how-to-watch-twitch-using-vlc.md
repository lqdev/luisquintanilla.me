---
title: How to watch Twitch streams using VLC
published_date: 2021-01-05 21:14:39
tags: [streaming, hacks, video, IT]
---

## Introduction

Twitch has become a tool for various creators to connect with audiences. Originally, streams were centered around gaming but now there is a wide variety of creators that include musicians, software developers, and craftspeople. One of my favorites is [Brainfeeder](https://www.twitch.tv/brainfeeder). I myself try (and often fail) to build software projects on stream. If you're interested in hanging out and talking machine learning, .NET and all things tech, feel free to join me at [https://twitch.tv/lqdev1](https://twitch.tv/lqdev1).

Watching Twitch streams on the desktop via the browser can take a toll on compute resources. A more efficient solution to this problem is to use VLC. VLC is a free, open-source, cross-platform media player that supports various protocols. While it was possible to watch Twitch streams using VLC before, it often required additional software such as [Streamlink](https://github.com/streamlink/streamlink). More recent versions make this requirement redundant. In this post, I'll show how to use VLC to watch Twitch streams.

![Twitch iOS App on iPhoneX - Caspar Camille Rubin](https://user-images.githubusercontent.com/11130940/103722126-4883f780-4f9d-11eb-8cc8-304d1a249ef9.png)

## Prerequisites

- VLC (version **3.11.0 or higher**)

## Watch Twitch Stream

> **Update 11/12/2021**  
> It's been a while since I've gotten the original steps to work, so feel free to skip them. You can still watch Twitch streams on VLC using Streamlink. To use Streamlink:
> 1. Download and install [Streamlink](https://streamlink.github.io/install.html)
> 1. Open the terminal and run:
>    
>    ```
>    streamlink https://twitch.tv/<username> best
>    ````
>
> For more information, see the [Streamlink Command-Line Interface documentation](https://streamlink.github.io/cli.html)

1. Open VLC
2. From the menu, Select **Media > Open Network Stream**
3. Paste the URL to the stream you want to watch in the text box (i.e. https://twitch.tv/lqdev1)

At this point, the stream should start playing.

### CLI

Alternatively on Unix (Mac / Linux) systems, you can use the CLI. To watch a stream, open a terminal and enter the following command:

```bash
vlc https://twitch.tv/<username>
```

Make sure to replace the username with the streamer you want to watch.

After a few seconds, VLC loads and starts playing the stream.

## Performance comparison

Below is a comparison of the same stream playing on the browser vs VLC. These test were performed on a Surface Laptop 1 running Windows 10. By no means should you take this as a definitive performance comparison as there are many variables that may affect performance. However, it's a nice way to visualize each of the methods.

### Browser

In the browser scenario, I only had a single tab playing the Twitch stream on Microsoft Edge (chromium).

![Browser task manager](https://user-images.githubusercontent.com/11130940/103723087-43c04300-4f9f-11eb-9969-df40541693bd.png)

### VLC

![VLC task manager](https://user-images.githubusercontent.com/11130940/103721933-e1fed980-4f9c-11eb-92f3-0147f2c09134.png)

As it can be seen from this image, there's about a 4x decrease in CPU resource consumption when using VLC.

## Conclusion

In this post I showed how you can use VLC to watch Twitch streams. One of the drawbacks is, you can't see the channels you follow and who's online. You also can't browse streams like you would on the Twitch web client. You also don't have access to chat, which is half the fun on many streams. However, if you're only interested in the content, VLC is an excellent and less resource-hungry way of watching Twitch streams. See you on Twitch!
