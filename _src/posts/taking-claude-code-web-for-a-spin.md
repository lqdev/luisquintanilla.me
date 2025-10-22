---
post_type: "article" 
title: "Taking Claude Code on the web for a spin"
description: "First steps with Claude Code for Web"
published_date: "2025-10-21 16:35 -05:00"
tags: ["ai","claudecode","programming","claude","anthropic","agents","rss","atom","browser","javascript","indieweb"]
---

Yesterday, Anthropic announced [Claude Code on the web](/responses/claude-code-on-the-web-2025-10-20), a way of using Claude Code in your browser.

I decided to try it out and within an hour, I'd built a new project from scratch. 

## What I built

As someone who uses RSS extensively, hunting for feeds buried in page source code is one of my least favorite tasks. Even when feeds are prominently displayed, I still have to open my RSS reader, copy the URL, and paste it into my app. 

Back in the day, [RSS support was built into the browser](https://openrss.org/blog/browsers-should-bring-back-the-rss-button). Sadly, those days are gone, so now you have to build those features yourself. However, doing so is straightforward thanks to browser extensions.

### RSS Browser Extension

Introducing [RSS Browser Extension](https://github.com/lqdev/rss-browser-extension), a lightweight browser extension for Chromium-based browsers (Chrome, Brave, Edge, etc.) that automatically detects RSS and Atom feeds on web pages and allows quick subscription via multiple RSS readers.

#### Discover

When you visit a site and a feed is discovered, the extension icon on the browser toolbar lights up and a badge shows the number of feeds detected.

![Browser Toolbar with RSS Browser Extension Highlighted](https://raw.githubusercontent.com/lqdev/rss-browser-extension/refs/heads/main/images/feed-discovery.png)

#### Subscribe

When you click on the extension icon, it opens a page displaying all discovered feeds.

![RSS Browser Extension Displaying Discovered Feeds](https://raw.githubusercontent.com/lqdev/rss-browser-extension/refs/heads/main/images/discovered-feeds.png)

From here, with a click, you can subscribe using a variety of RSS readers such as Newsblur, Feedly, Inoreader, and many others. 

## What I liked

- **Works on desktop and mobile** - Although I could only test the RSS Browser Extension on desktop, I was doing the prompting and coding on mobile. I like being able [to delegate work to AI coding assistants while on the go](/notes/copilot-add-new-features-but-first-coffee). 
- **Open in CLI** - I haven't tried this yet, but it's convenient to continue working in the terminal when I want to.
- **Easy to set up with GitHub Connector** - Anthropic's GitHub connector makes it easy to connect to your GitHub profile and repos.

## Improvements I'd like to see

- **Multi-modal capabilities** - During my AI-assisted coding sessions, one of the things I do when I run into issues is show rather than tell. Especially when it comes to UI, it's easier to just upload or reference images to show the AI assistant what needs to be fixed. Unfortunately, I couldn't find a way to do this.
- **Session management** - Part of this is on Anthropic and part on me. I wasn't sure how to best use sessions, but it seems you should start a new one per feature since each creates its own branch. What bothered me was that sessions aren't organized by repo, making it harder to manage multiple projects.
- **Only works with existing projects** - I had to select a repo before starting to prompt. What I'd prefer is to sketch out ideas first for a greenfield project, then create the repo afterward if I want to keep the code.
- **No MCP support** - I'm sure with time this will come, but I couldn't find guidance on how to set up MCP servers.
- **No Claude chat integration** - I often [brainstorm and sketch out ideas in chat](/posts/vibe-specing-prompt-to-spec), then use Copilot or Claude Code to implement the spec. I'd like to transition directly from chat to Claude Code on the web, or reference previous conversations as starting points.

## Conclusion

Overall I like being able to kick off jobs and have them running in the background as I go about my day. Claude Code on the web is a step in the right direction. Given it's still early days, I suspect many if not all of the items on my wishlist will eventually be addressed.

For now, I'm still partial to GitHub Copilot Coding Agent, mainly because of its seamless integration with GitHub. That said, I'm open to exploring Claude Code on the web further to see where it fits into my workflows as it matures.

In the meantime, feel free to use the [RSS Browser Extension](https://github.com/lqdev/rss-browser-extension/tree/main?tab=readme-ov-file#installation) and if you find it useful or run into issues, [send me a message](/contact).