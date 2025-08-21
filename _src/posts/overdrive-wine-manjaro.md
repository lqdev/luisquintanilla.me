---
post_type: "article" 
title: "Install OverDrive Media Console on Manjaro"
description: "Learn how to use Wine to install OverDrive Media Console on Manjaro and manage books and audiobooks from your local library."
published_date: "05/11/2022 19:22 -05:00"
tags: [linux, books, audiobooks, manjaro]
---

## Introduction

For the last few months, Manjaro has been the operating system I use as my daily driver for personal tasks. I still have my Surface Go tablet which runs Windows 10 that I use for tasks line syncing my Garmin watch and GPS. On one of the [posts in my feed](/notes/ipod-touch-discontinued), I briefly mentioned my audio solution for podcasts and audiobooks which mainly consists of a standalone MP3 player ([FiiO M6](https://fiio.com/m6)). 

For podcasts, I manage them through my [NewsBlur RSS feed reader](https://newsblur.com/). When I want to download them for offline listening, I've written a .NET console app ([podnet](https://github.com/lqdev/podnet)) to manage that process for me. Audiobooks are a little trickier. Public domain works I get through [Librivox](https://librivox.org/). The limitation there though is that I'm only public domain works are available. Therefore if I want other audiobooks, I either have to purchase them (which has its own issues because of DRM) or borrow them from my local library. I prefer to borrow my books and audiobooks from the library. Not only am I supporting my local library but I also save money in the process. 

Many libraries including my own manage digital assets through [OverDrive](https://www.overdrive.com/). OverDrive offers a variety of ways for transferring books and audiobooks to your devices. Linux however is not one of those solutions. Until February 2022, OverDrive provided a Windows desktop application (OverDrive Media Console) for managing books and audiobooks. Although there is no Linux desktop application, Linux has Wine. [Wine](https://www.winehq.org/) according to their website is "...a compatibility layer capable of running Windows applications on several POSIX-compliant operating systems, such as Linux, macOS, & BSD.". 

In this post, I'll show how you can use Wine to install OverDrive Media Console on a PC running the Manjaro Linux distribution. 

## Install Wine

In order to install OverDrive Media Console, you'll need to install Wine. To do so:

1. Open the terminal.
1. Install Wine and all its dependencies with the following command:

    ```bash
    sudo pacman -S wine winetricks wine-mono wine_gecko
    ``` 

## Install OverDrive Media Console

The next step is to install the OverDrive Media Console application for Windows desktop. 

1. Download the latest version of [OverDrive Media Console for Windows Desktop application](https://www.overdrive.com/apps/overdrive./). This should be a Microsoft Installer file with the `.msi` extension. Note that as of February 2022, the desktop application is [no longer available for install](https://help.overdrive.com/en-us/0733.html?tocpath=Home%7CGet%20help%20with%20the%20OverDrive%20app%7CWindows%20(desktop)%7C_____7).
1. Open the terminal and use Wine to install OverDrive Media Console with the following command:

    ```bash
    wine msiexec /i ODMediaConsoleSetup.msi
    ```

    Update the name of the `.msi` file as needed.  

## Run OverDrive Media Console

Once the installation is complete, OverDrive Media Console should be in your list of applications. Run it as you would any other app. 

![OverDrive Media Console running on Manjaro Linux](https://user-images.githubusercontent.com/11130940/167964715-bb8bfb21-5f72-4a86-9fb0-33c9d1fe9eae.png)

Here's a guide for information on getting started with [OverDrive](https://help.overdrive.com/en-us/categories/getting-started.htm).