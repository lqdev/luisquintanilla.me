---
title: Windows 10 Intel Display Driver Blank Screen Fix
date: 2017-12-09 14:27:31
tags: sysadmin,drivers,pc issues
---

Recently my Dell Inspiron P57G was experiencing issues when entering sleep mode. When trying to wake the PC, a blank screen was coming on. After some research, I concluded it was due to bugs with the most recent version of the display driver. These were the steps I took to remediate the issue. 

1. Go to [Dell Support](http://www.dell.com/support/home/us/en/04?app=drivers&c=us&l=en&~ck=mn) page and find the Intel HD Graphics 510 515 520 530 540 Driver.
2. Click 'Other Versions'
3. Download the most recent driver prior to 08/13/2017.
4. Follow the installation instructions and restart the PC.
    - If prompted to overwrite newer drivers, select 'Yes'

After restart, either force the PC to sleep or let it sleep on its own. The problem should've been fixed. 



