---
post_type: "note" 
title: "Fitch: CLI system information utility built with F#"
published_date: "2022-12-25 20:10 -05:00"
tags: ["dotnet","fsharp","linux","system","utilities","cli","fitch"]
---

I wasn't really planning on this, but here we are. I've been using [Neofetch](https://github.com/dylanaraps/neofetch) on my PC to display system information whenever the terminal opens. However, in some cases, that would hang and it would take a while before I was able to use my terminal. 

I then ran into [Nitch](https://github.com/unxsh/nitch) which is simple and fast. However, it was missing the local and public IP information Neofetch provides out of the box. Nitch is written using the [Nim](https://nim-lang.org/) programming language. Originally, I had planned on extending Nitch and I figured out the code I needed to write to support IP information. However, my program wouldn't build and I was unable to find any information online to unblock myself. 

That's when I looked at the existing components in Nitch and found that it was basically just reading files from the `/proc` and `/etc` system directories. At that point, I decided to see if I could build my own utility using F#. A couple of hours later and the result was [Fitch](/github/fitch). This works well enough for my needs and in the future, I might publish it as a dotnet global tool to make it easy for others to use. 

![Screeenshot of command line running Fitch F# System Utility](https://cdn.lqdev.tech/files/images/fitch-display.png)