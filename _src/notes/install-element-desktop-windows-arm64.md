---
post_type: "note" 
title: "Install Element Desktop on Windows ARM64 Devices"
published_date: "2025-04-07 08:38 -05:00"
tags: ["matrix","arm64","windows","element","winget","opensource","decentralization"]
---

In a previous post, I talked about my [workaround for running Element Desktop on my ARM64 Windows device](/notes/running-linux-gui-apps-windows-wsl-amazing).

I'm happy that I no longer need that workaround. 

[Element Desktop](https://github.com/element-hq/element-desktop) enabled support for [ARM64 on Windows](https://github.com/element-hq/element-desktop/pull/624) and shipped it as part of the [1.11.95 release](https://github.com/element-hq/element-desktop/releases/tag/v1.11.95).

While you could go to the [downloads website](https://element.io/download) and get the installer from there, you can now also get it via [WinGet](https://learn.microsoft.com/windows/package-manager/winget/). 

I built on the work of the [existing community manifest](https://github.com/microsoft/winget-pkgs/tree/master/manifests/e/Element/Element) in the winget-pkgs repo and via a [pull request](https://github.com/microsoft/winget-pkgs/pull/244768) updated the latest 1.11.96 version to include the ARM64 installer. 

While I didn't create the manifest from scratch, I thought it was really easy to contribute a new installer. I'll leave the details of my process for another post though. 

Installing Element Desktop via WinGet is relativaly straightforward.  

Assuming you already have the WinGet tool installed, all you have to do is run the following commmand in the terminal and you're all set!

```powershell
winget install Element.Element -a arm64
```