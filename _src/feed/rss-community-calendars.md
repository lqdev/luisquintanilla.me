---
post_type: "note" 
title: "Shared calendars and RSS"
published_date: "2023-11-11 13:34"
tags: ["rss","calendars","protocols","community"]
---

I just came across a new use for RSS - shared calendars. Well, not exactly sharing calendars in RSS format. While I have seen calendars in RSS format as I'll mention later in this post, I think there's enough downsides that make this impractical. However, for discoverability, there's some RSS patterns that shared calendars could benefit from.  

While reading a [recent post on Doc Searls' blog](https://doc.searls.com/2023/11/09/datepress/) it got me thinking about using public calendars for sharing community events. By leveraging iCal or CalDav, this could be a good alternative to current solutions like Facebook Events or Meetup. It could also be a great way to foster interoperability. There's still the discoverability problem, but platforms like Meetup and Facebook Events could enable subscribing to shared public calendars.

This sent me down a search to see whether others were using shared calendars, similar to [The Big Calendar](https://bsquarebulletin.com/test-calendar/?r34icsym=202312). It was nice to see that Penn State has shared calendars for their various sports. That shouldn't be surprising. As a university, they already share their academic calendars. The part that I found surprising though was that RSS was one of the options. 

^^^
![Penn State Subscription Calendar Subscriptions](/assets/images/feed/rss-community-calendars.png)
^^^ Source: *gopsusports.com*

Downloading the RSS feed enabled me to import it into my feed reader and view it there. I'd prefer if instead of downloading the RSS feed as a file, they just provided the link so I could copy and paste it into my feed reader. However, it's still nice that just like podcasts and YouTube feeds, I can now also see Penn State athletic events in my feed. 

I'm not sure what the benefit of subscribing to this feed via RSS as opposed to the calendar itself has. The main drawbacks I see are that I can't get notifications or see it in my calendar. I also don't know if changes and updates to events also propagate to the RSS feed. That's why I'd prefer to just subscribe to the calendar directly. 

Going back to the broader topic of shared calendars and discoverability mentioned in the [DataPress proposal](https://bsquarebulletin.com/test-calendar/?r34icsym=202312) for community calendars, you could imagine a solution similar to how websites can make their [RSS feeds discoverable](https://blog.jim-nielsen.com/2021/automatically-discoverable-rss-feeds/). As a calendar and community maintainer, you could post the link to your shared calendars on your website. As a community member, you'd just need to point your calendar client to the respective website's domain (i.e. gopsusports.com) and your client would automatically import the shared calendars. This is how shared calendars work today. The difference is, you don't need the exact link to the calendar, you just need the website domain. The calendar client is what handles the discovery and import process.  

One important thing to highlight, doing something like this isn't exclusive to communities. You could imagine businesses being able to implement similar practices to communicate their business hours and availability. Platforms like Yelp or Google could then just import the public calendar for that business and display the business hours on their platform. To schedule your next oil change, imagine looking at your auto service shop's shared calendar and sending a calendar invite for one of their open slots. This is no different to solutions like [Microsoft Bookings](https://www.microsoft.com/microsoft-365/business/scheduling-and-booking-app). The difference is, instead of using these platforms, you would do it from your preferred calendar client of choice. 

You could also see this working for individuals as well. If you need to coordinate something with other people, instead of the constant back and forth to find availability, you can just share your public calendar and they can find a time that works for them. 

Anyway, the point of this longish post is that by building on simple components like calendars, iCal, CalDAV, and RSS, communities, businesses, and individuals could communicate and collaborate more effectively without being constrained to any single platform. 