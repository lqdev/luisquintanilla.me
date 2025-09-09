---
post_type: "article" 
title: "Building a Reading Habit with Audiobooks, LibriVox, and AntennaPod"
description: "Build up the reading habit by listening to free audiobooks on your phone using LibriVox and AntennaPod"
published_date: "2025-01-05 21:04 -05:00"
tags: ["book","audiobook","rss","antennapod","librivox","android","podcast","publicdomain","opensource"]
---

Last year, I was able to complete my goal of reading 12 books or roughly one book per month. I kept track of some of the books I read in my [library page](/reviews).

I enjoyed most of the books I read. However, despite hitting my goal and averaging a book a month, I didn't build up a habit which is what I would've preferred. Many of the books I read last year, I read them during long periods of uninterrupted time, such as cross-country flights. The topic of goal setting and measuring the right thing could be an entire post, but I'll leave that for another time. 

Taking the learnings from last year, this year, I'll try to focus on building a reading habit rather than reading a set number of books. While I'd still like to finish at least one book per month, that's a metric I'll use to track general progress rather than making it the goal. Unless I'm reading something like War and Peace, I'd expect that by establishing a consistent cadence, I should be able to get through at least one book every month. Also, I'm only accounting for uninterrupted reading time blocks. Using audiobooks, I can continue building my reading habit while working out or doing chores. Kind of like having TV playing in the background. 

There is no shortage of audiobooks out there. Audible is one of the easiest ways to access audiobooks and they have a large selection to choose from. Personally, I don't use Audible. I don't like paying for a separate subscription, getting credits to "buy" books that I don't really own and can only listen through the Audible app. Instead, I use Spotify. I get [15 hours of access to audiobooks per month](https://support.spotify.com/us/article/audiobooks-access-plan/) as part of my subscription. While I still don't own the books and can only listen on the Spotify app, at least I'm not paying a separate subscription. Also, the 15 hour limit at this time is more than enough for me. 

The library is another amazing option. No subscription fees and you get access to your local library's entire digital catalog. Using the [Libby app](https://www.overdrive.com/apps/libby), you can borrow books and start listening for free. Selection though may be an issue here since you are limited to what's available through your local library. Pro-tip, universities and employers sometimes have libraries, so as an alumnus or employee, you can expand the number of books available to you by registering multiple libraries on the Libby app. Regardless of which library you choose though, you still have to go through the Libby app. 

The dream setup for me is, having direct access to the audiobook files so I can listen to them whenever and wherever I'd like. Similar to podcasts. Thanks to LibriVox and AntennaPod, that setup is possible. 

In case you're interested in setting up something similar, in this post, I'll briefly explain what LibriVox and AntennaPod are and show how you can use them to start listening to free audiobooks on your phone. 

## What is LibriVox?

[LibriVox](https://librivox.org/) is a website that provides a collection of free audiobooks from the public domain. Thanks to a vibrant community of volunteers, you can enjoy classics such as:

- [The Divine Comedy](https://librivox.org/the-divine-comedy-by-dante-alighieri/)
- [The Adventures of Sherlock Holmes](https://librivox.org/the-adventures-of-sherlock-holmes/)
- [Discourses of Epictetus](https://librivox.org/discourses-of-epictetus-by-epictetus/)
- [Roughing It](https://librivox.org/roughing-it/)
- [And many others...](https://librivox.org/search?primary_key=0&search_category=title&search_page=1&search_form=get_results&search_order=catalog_date)

It's important to note that the catalog is limited to public domain works. That means you won't have access to the latest self-help, business, or current events books. However, there are still thousands of books to keep you busy for an entire lifetime and the list is constantly growining since there are new works introduced into the public domain every year. 

Here's [a few notable literary works that entered the public domain in 2025](https://blog.archive.org/2025/01/01/welcome-to-the-public-domain-in-2025/) as well as the complete list of [literary works from 1929](https://archive.org/details/internetarchivebooks?tab=collection&query=date%3A1929).

## What is Antennapod?

[AntennaPod](https://antennapod.org/) is an open-source podcast player for Android. 

The app has a ton of great features, such as [Podcast Index](https://podcastindex.org/) support, subscribe via RSS directly,  OPML import and export, chapters, adjustable playback speed, and for the past few years, a personalized yearly summary which is computed locally on your device [(here's my summary from 2024)](/notes/antennapod-echo-2024).

You can get AntennaPod from the Play Store or [F-Droid](https://f-droid.org/en/packages/de.danoeh.antennapod/)

## Keep track of new releases

LibriVox provides [several feeds](https://librivox.org/pages/librivox-feeds/). 

Since podcast distribution is primarly done through RSS and you can subscribe to podcasts directly using RSS feeds in AntennaPod, we can use the app to stay up to date with new audiobook releases. 

To subscribe to the LibriVox New Release feed:

1. Copy the LibriVox New Releases RSS feed URL to your clipboard: [https://librivox.org/rss/latest_releases](https://librivox.org/rss/latest_releases)
1. In AntennaPod, open up the hamburger menu and select **Add Podcast**.
1. In the *Add Podcast* dialog, select **Add podcast by RSS address**.

    ![AntennaPod Add Podcast Dialog](https://cdn.lqdev.tech/files/images/add-podcast-antennapod.png)

1. Paste the LibriVox New Releases RSS feed URL into the text box and select **Confirm**. 

At this point, you should see the feed listed in your AntennaPod subscriptions. 

By subscribing the LibriVox New Release feed, AntennaPod treats the feed as a podcast, and each new release entry or audiobook is like a podcast episode. 

For more information on subscribing to podcasts, see the [AntennaPod documentation](https://antennapod.org/documentation/getting-started/subscribe). 

## Add books to Antennapod

Subscribing to the new releases feed only notifies you when new books are added, you can't listen to or download the books. To do that, you'll need to subscribe to the RSS feed of the book you want to listen to in AntennaPod.

### From New Releases Feed

1. In AntennaPod's Subscription screen, select **LibriVox's New Releases**.
1. From the list of new releases, select the book you want to listen to. For example, "Room of One's Own" by Virginia Woolf.

    ![AntennaPod New Releases Feed](https://cdn.lqdev.tech/files/images/antennapod-librivox-new-releases.png)

1. In the audiobook's screen, select **Visit Website**. This will take you to the LibriVox audiobook's page in the browser.

    ![AntennaPod Audiobook Screen](https://cdn.lqdev.tech/files/images/antennapod-room-of-ones-own.png)

1. In the LibriVox audiobook page, select the book's RSS Feed.

    ![A Room of One's Own by Virginia Woolf LibriVox book page](https://cdn.lqdev.tech/files/images/librivox-room-ones-own.png)

1. Copy the book's feed URL to your clipboard.
1. In AntennaPod, open up the hamburger menu and select **Add Podcast**.
1. In the *Add Podcast* dialog, select **Add podcast by RSS address**.
1. Paste the book's RSS feed URL into the text box and select **Confirm**. 

### Any other books

The process works the same for other books but you'll have to use LibriVox as the entrypoint to browse books.

1. In your browser, navigate to LibriVox. 
1. Go to the audiobook page. For example, this is the page for [Walden](https://librivox.org/walden-by-henry-david-thoreau).
1. In the audiobook page, select the book's RSS Feed. For Walden, it's [https://librivox.org/rss/549](https://librivox.org/rss/549).
1. Copy the book's feed URL to your clipboard.
1. In AntennaPod, open up the hamburger menu and select **Add Podcast**.
1. In the *Add Podcast* dialog, select **Add podcast by RSS address**.
1. Paste the book's RSS feed URL into the text box and select **Confirm**. 

## Conclusion 

Although AntennaPod wasn't designed for audiobook listening, thanks to the flexibility of RSS feeds and the thankless work of the LibriVox community, you can easily listen to audiobooks for free on your mobile device. I plan on testing out this setup and seeing whether it helps keep my reading habit on track.