---
title: "Get Podcast RSS Feed URL from Apple Podcasts"
language: "Javascript"
tags: "js,rss,web,apple,podcast"
created_date: "2025-11-17 23:26 -05:00"
---

## Description

Bookmarklet to extract RSS feed URL from Apple Podcasts

## Usage

### Add bookmarklet

1. Add to bookmarks
1. Paste snippet code into URL

### Get RSS feed

1. Go to Apple podcast show URL (i.e. [Radio Rental](https://podcasts.apple.com/us/podcast/radio-rental/id1483289230))
1. Click on bookmarklet

## Snippet

```javascript
javascript:(function(){var id=window.location.pathname.match(/id(\d+)/);if(id){fetch('https://itunes.apple.com/lookup?id='+id[1]+'&entity=podcast').then(r=>r.json()).then(d=>alert(d.results[0].feedUrl))}})();
```