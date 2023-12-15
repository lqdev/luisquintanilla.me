---
post_type: "note" 
title: "Have you tried turning it off and on again? - Mastodon edition"
published_date: "2023-12-15 15:53"
tags: ["mastodon","sysadmin","selfhost","fediverse"]
---

My Mastodon instance crashed this morning due to a combination of the usual running out of space issue and me being too lazy to remember to remove federated media regularly from my instance BEFORE it crashes. Now I know you can schedule this with CRON jobs but for some reason I've never been able to configure that correctly nor have I been motivated to get to the bottom of it.

Anyway, I went through the usual dance of starting up my services and [using the CLI to clean up my instance](/wiki/mastodon-server-cleanup).

However, none of my commands worked because it couldn't connect to the database. A quick look at the status showed the database and all my services were running which led me to go down a troubleshooting rabbit hole with no end in sight and little guidance as to what might be the problem.

Eventually, before gave up I thought, "my instance is down anyway, couldn't I just turn it off and on again?" I did exactly that and when it restarted I was able to continue with my cleanup tasks as normal.

So as a reminder, if all else fails, try turning it off and on again. 