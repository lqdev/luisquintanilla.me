---
post_type: "wiki" 
title: "Configure opacity - Manjaro"
last_updated_date: "12/11/2022 18:56 -05:00"
tags: i3,wm,linux,manjaro,kitty,terminal,wiki,configuration,picom
---

## Overview

This article shows how to configure background opacity for the kitty terminal emulator using picom and i3. 

By defaut, i3 doesn't support composting. Programs like picom add support for it. 

In addition, terminal emulators like kitty are hardware accelerated which means there's opportunities to offload from the CPU and save power.  

## Install & configure picom

1. Install picom

    ```bash
    sudo pacman -S picom
    ```

1. Move default configuration file to *~/.config/picom/picom.conf*
1. Define opacity rule: 

    ```bash
    opacity-rule = ["90:class_g = 'kitty'"];
    ```

1. Save changes

## Install & configure kitty

1. Install picom

    ```bash
    sudo pacman -S kitty
    ```

1. Move default configuration file to *~/.config/kitty/kitty.conf*
1. Set background opacity: 

    ```bash
    background_opacity 0.90
    ```

1. Save changes

## Configure i3

1. Open your i3 configuration and add the following line to it:

    ```bash
    exec_always picom
    ```

1. Restart i3.

## References

- [Picom](https://wiki.archlinux.org/title/Picom#Opacity)
- [Kitty](https://wiki.archlinux.org/title/Kitty)
- [Kitty emulator not working - Reddit](https://www.reddit.com/r/i3wm/comments/g62qy0/kitty_terminal_emulator_transparency_not_working/)