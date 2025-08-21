---
post_type: "article" 
title: HP ProBook 430 G1 Ubuntu BIOS Upgrade
published_date: 2017-12-09 12:57:12 -05:00
tags: [sysadmin,ubuntu,bios]
---

I recently got an HP ProBook 430 G1 which came preinstalled with Windows 10. I have several Windows 10 devices, but needed a Linux system while on the go. I had given my HP Chromebook a try using Crouton. This worked okay. However, I was limited by the specs of the Chromebook and wanted something with more power for development purposes. Therefore, I erased Windows 10 on the ProBook and installed Ubuntu. The PC worked well. My only problem with it was that I found my battery meter was inaccurate and would reach a percentage in the 60’s before suddenly dropping to zero and shutting down. This was not something I could have, especially during the middle of a work session where I might lose some/all of my work.

Immediately, I assumed it had something to do with drivers/BIOS being out of date. To my surprise, BIOS updates were only available for Windows. Fortunately, through some research I was able to find a solution that allowed me to update my BIOS and fix my battery problems.

## Prerequisites:

1. Make sure PC is connected to the charger
2. 4GB USB Drive

## Procedure:

1. Format USB drive. (FAT32 required)
2. Create folder inside USB drive called Hewlett-Packard\BIOS\New
3. Download latest BIOS .exe file from HP Support website
4. Unzip .exe file. While 7Zip is not required, I found this to work well.
5. Copy .bin file to Hewlett-Packard\BIOS\New folder
6. Copy entire BIOSUpdate folder to Hewlett-Packard folder.
7. Restart PC
8. Continuously press F10 to enter BIOS Setup
9. Select Update System BIOS menu option
10. Select Update BIOS Using Local Media option
11. Select Accept

At this point, the installation process will take effect and the PC will restart itself at which point the your BIOS should be up to date.
