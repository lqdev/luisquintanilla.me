---
post_type: "note" 
title: "RSS for new event notifications"
published_date: "2023-11-12 09:52 -05:00"
tags: ["rss","calendars","protocols","community"]
---

After my post on [Shared calendars and RSS](/notes/rss-community-calendars) I kept thinking about what else you can get out of sharing a calendar using RSS. What I didn't see immediately is the main use case of RSS, though I did mention it as a drawback.

> The main drawbacks I see are that I can't get notifications or see it in my calendar.

Actually, with RSS, I *can* get notifications when a new event has been added to the calendar. With iCal for example, when a new event is added to the calendar, it'll automatically be populated in my calendar client. However, I won't get a notification that a new event has been added. Conversely, with RSS, I won't see it in my calendar client but I will get a notification in my RSS reader. 

When paired together though you could use RSS to get notified when a new event is added to the calendar and also be able to view it in your calendar client since it'll automatically be populated (assuming you're already subscribed to the shared calendar). That means managing two feeds, but the more I think about it, the RSS format makes some sense. The shared calendar could be the source of truth. When a new event is added to the calendar, the RSS feed can be updated. The `channel`property has the link to the shared calendar you can subscribe to and the individual events have *.ics* files embedded in the `enclosure`. As a consumer, the new event notification will then appear in my RSS reader.   