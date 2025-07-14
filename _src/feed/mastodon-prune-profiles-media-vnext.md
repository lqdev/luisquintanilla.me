---
post_type: "note" 
title: "Removing avatars and headers in Mastodon"
published_date: "2022-12-26 17:00"
tags: ["mastodon","fediverse","selfhost","sysadmin"]
---

The next version of Mastodon can't come soon enough. 

About 20GB of disk space on my instance are being taken up by media from remote users. And this is for a single user instance! I can't imagine how it must be on other instances with multiple accounts. 

![Command line displaying disk space used by Mastodon for media](/assets/images/feed/mastodon-media-usage.png)

A recent [PR](https://github.com/mastodon/mastodon/pull/22149) should make things much better though by allowing you to remove these files. 

The only thing you need to do is pass the `--prune-profiles` flag to the `media remove` CLI command. 

The PR is already merged so hopefully that means it's coming the next release. 
