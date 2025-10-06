---
post_type: "article" 
title: "Introducing Podcast TUI"
description: "An open-source, cross-platform Terminal User Interface (TUI) for podcast management built with Rust"
published_date: "2025-10-05 21:44 -05:00"
tags: ["podcast","rss","rust","tui","terminal","opensource"]
---

I'm excited to introduce [Podcast TUI](https://github.com/lqdev/podcast-tui), an open-source, cross-platform Terminal User Interface (TUI) for podcast management built with Rust.

![Podcast TUI v1.0.0-mvp initialization screen with retro terminal design](https://cdn.lqdev.tech/files/images/podcast-tui-splash.png)

## Why?

### The Dedicated Device Philosophy

Over the past few years, I've shifted to [using](/uses) dedicated devices rather than using my phone for everything. Part of that was driven by upgrading to a flip phone. I've since gone back to using a smartphone, but I still use a dedicated MP3 player. 

There's three main reasons:

- **Media ownership** - I've shifted towards owning my own media. That means [music, movies, TV shows, and books](/notes/am-i-in-2006-newspapers-cds-mp3s/). Call me crazy, but I think [if you pay for something, you own it](/responses/landlord-of-your-notes/). Also, I often end up watching the same things over and over as background noise like The Office or Parks and Rec, so it saves me money on subscription fees.  
- **No distractions** - If I want to listen to music or a podcast, I'm not distracted by other apps on my smartphone.
- **Battery life** - MP3 players tend to have longer battery life, so not only can I consume content for longer, but I also don't have to charge as often. Plus, I don't drain my phone's battery.

### The MP3 Player Journey

I started with the [FiiO M6](https://www.fiio.com/m6). It was compact, and since it ran Android, I could sideload F-Droid and Antennapod for podcast management. Sadly, it was painfully slow.

About a year ago, I upgraded to a [Hiby R4](https://store.hiby.com/products/hiby-r4). Larger screen, newer Android, more robust specs. I still use it today, but it's built like a tank. It's about as thick as the original Walkman and heavy. It's not very portable.

The real issue with these Android-based MP3 players is they're basically smartphones without a modem. I can load the same distracting apps on them. My Hiby has YouTube and a browser, which defeats the whole "dedicated device" purpose.

About a week ago, I got the [Innioasis Y1](https://www.innioasis.com/products/y1) player. Quick impressions:

- Love the look. Looks like an iPod.
- Small and lightweight.
- **No Android**
- Good enough battery life (~50% drain after a week, mostly on standby).

The build quality and UI could use work, but for $50, I'm not complaining.

### The Problem

Because the Innioasis Y1 isn't yet another Android device, I can't sideload Antennapod and have to manage podcasts manually. 

A few years ago, I faced this same problem and built [podnet](https://github.com/lqdev/podnet), a simple command-line podcast manager built with F#. When I did research back then, there was nothing that worked the way I wanted.

Fast forward 3 years, and there still isn't a great cross-platform solution that's actively maintained. At least not one that:

- Works well on both Windows and Linux
- Relies on open standards like RSS and OPML
- Has good metadata support
- Actually does what I need without being bloated

Given what I learned from building podnet, I decided to build a more robust version. I didn't want to spend a ton of time on it though, so as I'll talk about later in the "How it was built?" section, I used AI to help. But first, let me show what the app does. 

## Features

### Easy navigation using buffers

![Buffer List menu showing navigation options: Help, Podcasts, Downloads, What's New](https://cdn.lqdev.tech/files/images/podcast-tui-buffer-list.png)

### Add or remove podcast feeds

![Podcasts screen displaying 6 subscribed podcasts with their full names](https://cdn.lqdev.tech/files/images/podcast-tui-podcast-list.png)

### Browse podcasts episodes

![List of 10 Windows Weekly podcast episodes with titles and publication dates](https://cdn.lqdev.tech/files/images/podcast-tui-episode-list.png)

### Download podcasts

![Downloads screen showing 14 downloaded podcast episodes with checkmarks](https://cdn.lqdev.tech/files/images/podcast-tui-downloads.png)

### Show the last 100 episodes from feeds

![Podcast TUI showing What's New screen with 100 recent podcast episodes](https://cdn.lqdev.tech/files/images/podcast-tui-whats-new.png)

## How it was built?

### The Tech Stack (Sort Of)

The application is built entirely in Rust. Outside of the [getting started tutorials](https://rust-lang.org/learn/get-started/), I've never built anything in Rust before.

### The AI-Assisted Approach

I stumbled my way through building this app using a combination of:

- [DeepWiki](https://docs.devin.ai/work-with-devin/deepwiki-mcp) - For analyzing existing Rust projects
- [Context7](https://github.com/upstash/context7) - For library documentation
- [Perplexity Ask](https://github.com/perplexityai/modelcontextprotocol/) - For research and problem-solving
- [Claude Sonnet 4.5](/responses/introducing-claude-sonnet-45-2025-09-29) in [VS Code Copilot Agent mode](https://code.visualstudio.com/docs/copilot/chat/chat-agent-mode)

I started with an initial [requirements document](https://github.com/lqdev/podcast-tui/blob/main/docs/PRD.md). Because I'm not familiar with Rust, I was mostly going on vibes though so you could say the app was entirely **vibe-coded**. As I tested and validated functionality, I strayed from the original requirements, following what felt right rather than what I'd planned.

### What I Learned

Not being familiar with the stack was actually a pro in this case. It forced me to focus on the **"what"** rather than the **"how"**. 

If I was building with .NET or Python, I would've been more opinionated about implementation details. Instead, by focusing on what I wanted, I spent more time thinking about whether the experience felt right. For a personal project, I think that's perfectly fine. If this was going into production with users depending on it, I'd pay more attention to the "how" and whether it's effectively achieving everyone's goals.

If I were to do it again, I'd spend more time keeping my requirements document and project documentation up to date. Better documentation would help the AI coding assistant converge faster on solutions.

### Timeline

I went from idea to MVP in about a week.

The core functionality was working in a few hours. The rest was polish and improvements, mostly done during the weekend. 

## What's next?

I'm happy with the progress so far, but there's still work to do.

### Priorities

These are the things I definitely want to add:

- **Repo cleanup** - The repo grew organically throughout the coding sessions. I need to clean up the documentation and refactor the code to follow best practices.
- **OPML support** - Right now there's no way to import or export subscriptions using OPML. This is pretty essential for portability.
- **Playlist support** - Being able to create and manage playlists would make the listening experience much better.
- **Pre-built executables** - I want to build and publish pre-built executables for the various OS platforms so people don't have to compile from source.

### Nice-to-Haves

These would be cool additions if I get around to them:

- **Podcast 2.0 Support** - I haven't dug much into this ecosystem in a while, but there's been a lot of progress in recent years. I'd like to see if there are opportunities to support the standards being introduced by [Podcasting 2.0](https://podcasting2.org/).
- **Device Sync** - When I mount my MP3 player, I only want to transfer downloaded files that aren't already on the device. Today, I manually copy-paste directories. It would be nice to handle this through the app.

### What I'm Not Building

Some features I'm intentionally leaving out:

- **Playback** - I don't care about playback. I don't need to play files inside the terminal. I can just use my MP3 player or whatever's on the device I'm using, which is usually VLC or MPV. 

## Try it out

If this sounds remotely useful to you, grab the app from the [repo](https://github.com/lqdev/podcast-tui) and try it out. 

This is an MVP, so I expect some things not to work. Currently it works for what I need, and hopefully it works for you too.

If you find a bug or have ideas for improvement, I'd love to hear from you. [File an issue](https://github.com/lqdev/podcast-tui/issues) or [send me an e-mail](/contact).
