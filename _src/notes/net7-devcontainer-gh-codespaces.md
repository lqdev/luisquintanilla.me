---
post_type: "note" 
title: "Configured GitHub Codespaces Dev Container with .NET 7"
published_date: "2022-11-21 11:28 -05:00"
tags: ["dotnet","github","codespaces","development","devcontainer","website","internet"]
---

Soon after .NET 7 was released, I [upgraded](/notes/net7-website-update) the static site generator I use as well as the GitHub Actions that build and publish my website. Having upgraded last year from .NET 6, the process was as smooth as I had expected with no code refactoring required. 

When it comes to development environments, for quick status updates like the ones on my [feed](/feed) or minor edits, I've been using [github.dev](/notes/surface-duo-blogging-github-dev). However, there's been times where I've needed to run and debug code to confirm that my changes work. This is where I hit some of the limitation of github.dev which means unless I set up a Codespace, I have to save my work and move offline to my PC. Codespaces are great, but given that Codespaces are nice to haves not a requirement for my workflow at this time, it didn't make sense for me to pay for them. That's why I was excited to learn that GitHub is providing up to [60 hours per month of free Codespace usage](https://github.blog/2022-11-10-whats-new-with-codespaces-from-github-universe-2022/) to all developers. That's more than enough for me. 

![Blog post authored in GitHub Codespaces with integrated terminal open](/api/files/images/net7-gh-codespaces.png)

By default, Codespace images come preinstalled with the .NET 6 SDK which makes sense considering it's the latest LTS. However, since my static site generator targets .NET 7, I had to configure my Codespace to use .NET 7. This was just as easy as upgrading to .NET 7. All I had to do was provide the .NET 7 SDK Docker image as part of my [devcontainer.json](https://github.com/lqdev/luisquintanilla.me/blob/main/.devcontainer.json) configuration file. From there, Codespaces takes care of the rest. As a result, I can now run and debug my code all in one place without interrupting my workflow. 

PS: This post was authored in GitHub Codespaces :slightly_smiling_face: