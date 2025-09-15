---
post_type: "note"
title: "Copilot, add new features. But first, coffee"
published_date: "9/14/2025 10:31 PM -05:00"
tags: ["github","copilot","ai","indieweb","blogging","githubactions"]
---

Something I've tried to make a habit of on a weekly basis is compiling a post which aggregates all posts on my website from the past week. Because right now it's not fully automated and I haven't been posting as much, there are gaps.

The process is fairly straightforward. I run a script that:

- Collects all of the previous week's posts
- Drafts a note with links to all the posts

Then, I publish the post. 

Given this is mostly automated, I decided it was something I could just create a GitHub Action to do the post generation and publishing on my behalf. 

However, there were some issues with the original file generation and formatting that were annoying, but easy enough to fix manually so I never invested time in fixing them.

This morning, I created a few issues describing the features and improvements I wanted to make. Worth noting, I did this from my phone. 
 
- [#213](https://github.com/lqdev/luisquintanilla.me/issues/213) 
- [#215](https://github.com/lqdev/luisquintanilla.me/issues/215)

Rather than working on them though, I assigned them to GitHub Coding Agent and stepped out to grab coffee.

The agent worked on solutions in the background and by the time I got back, I had working solutions. They weren't perfect but with no input from me, it got 90-95% of it right. The rest were minor refinements.