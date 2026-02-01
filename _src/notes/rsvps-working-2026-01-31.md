---
post_type: "note"
title: "RSVPs working"
published_date: "1/31/2026 11:51 PM -05:00"
tags: ["rsvp","indieweb","webmentions","activitypub","fediverse","events"]
---

Nice! I got RSVP posts working on my site. This works with Webmentions / IndieWeb

<img width="1040" height="418" alt="Image" src="https://github.com/user-attachments/assets/0f841b13-43d2-4b6e-af63-432bc1adb7e9" />

<img width="1222" height="576" alt="Image" src="https://github.com/user-attachments/assets/02c3c7c8-f60b-4233-993e-4ade50de24f7" />

as well as ActivityPub

```json
   {
      "@context": "https://www.w3.org/ns/activitystreams",
      "id": "https://lqdev.me/api/activitypub/activities/17ca544b8fb87ea6223e2f5b0cad040e",
      "type": "TentativeAccept",
      "actor": "https://lqdev.me/api/activitypub/actor",
      "published": "2026-01-31T23:43:00-05:00",
      "to": [
        "https://www.w3.org/ns/activitystreams#Public"
      ],
      "cc": [
        "https://lqdev.me/api/activitypub/followers"
      ],
      "object": "https://events.indieweb.org/2026/02/homebrew-website-club-pacific-MyM39P5egEsp",
      "inReplyTo": "https://events.indieweb.org/2026/02/homebrew-website-club-pacific-MyM39P5egEsp"
    }
```