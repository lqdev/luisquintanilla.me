---
post_type: "note" 
title: "Running Linux GUI Apps on Windows (WSLg) is amazing!"
published_date: "2024-11-27 20:48 -05:00"
tags: ["wsl","linux","windows","gui","element","matrix"]
---

So you might've heard of Windows Subsystem for Linux (WSL), but did you know you can also [run Linux GUI apps with WSLg](https://learn.microsoft.com/windows/wsl/tutorials/gui-apps)?

While trying to install the Element Desktop client on my Spandragon X Elite Windows device, I quickly realized [you can't because of an issue with Electron](https://github.com/element-hq/element-desktop/issues/650).

WSL is supported on ARM64 Windows devices. This means, it should also support GUI apps. 

I decided to try and install the Element Desktop client for Linux in WSL and this was the result!

![Screenshot of Element Desktop Login Screen](/files/images/element-linux-wsl.png)