---
post_type: "note"
title: "Website now natively posts to the Fediverse"
published_date: "1/22/2026 1:17 PM -05:00"
tags: ["fediverse","posse","indieweb","mastodon","githubactions","azure"]
---

Success! I got my website configured to post to the Fediverse. I was previously doing [POSSE (Post on your Own Site Sindicate Elsewhere) via RSS and Azure Logic Apps](/posts/rss-to-mastodon-posse-azure-logic-apps/), that was just posting links to posts on my website and I had to run a separate Mastodon server. Now, the post itself is a Note in the Fediverse and my website itself is a node on the Fediverse. That means I should be able to get rid of my Mastodon instance. Since it was a single-user instance, this solution works just as well if not better. It's a bit hacky for now but I got it working by stitching together Azure Functions APIs in Azure Static Web Apps and cron jobs using GitHub Actions. 

Here's what a [recent post](/responses/spotify-is-testing-a-feature-that-syncs-audiobooks-2026-01-22/) looks like in the Mastodon timeline.

<img width="717" height="1065" alt="Image" src="https://github.com/user-attachments/assets/e20eddc4-d256-4480-a2cd-bb35053a94c4" />