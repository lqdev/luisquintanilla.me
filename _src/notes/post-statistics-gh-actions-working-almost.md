---
post_type: "note" 
title: "GitHub Actions workflow for post metrics almost works"
published_date: "2024-01-03 00:26 -05:00"
tags: ["github","cicd","blogging","automation","githubactions","fsharp","cron"]
---

A few days ago, I posted about a script I wrote. The [script computes computes post metrics on my website](/posts/website-metrics-github-actions/). That's what I used to author my [(We)blogging Rewind 2023](/notes/weblogging-rewind-2023-continued/) posts. Because I want the script to run at periodic intervals, I automated the process using GitHub Actions. 

The script is scheduled to run on the first of every month and I'm happy to report that it works! Kind of.

![Image of an unsuccessful run of a GitHub Actions workflow](https://github.com/lqdev/luisquintanilla.me/assets/11130940/bd413326-cc7b-44f7-a287-4580f1268cb2)

The GitHub Action was successfully triggered at the defined interval. Unfortunately, the script failed due to a [user error which I've since fixed](https://github.com/lqdev/luisquintanilla.me/commit/1296c3f929e3ad2834a8b7963c9ed66421a4fb8a). Come February 1st, everything should work as expected though, so I'm happy about that. 
