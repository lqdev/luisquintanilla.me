---
post_type: "article" 
title: "Install Manjaro RTL8821CE WiFi drivers"
description: "Install RTL8821CE WiFi drivers on a fresh Manjaro XCFE install using AUR"
published_date: "06/11/2022 15:48 -05:00"
tags: [networking, manjaro, linux]
---

Until recently, I was usign an HP ProBook 430 G1 with Manjaro and a Surface Go (1st Gen) for my computing needs. To cut down on the devices I used, I purchased an [ASUS L210MA](https://www.asus.com/us/laptops/for-home/everyday-use/asus-l210/) which is about the size of the Surface Go but I can run Manjaro on it. Although it doesn't have the same storage or compute power as the ProBook, I've set up a VM in Azure I can use for those instances where I need more compute power than the ASUS laptop can provide. As is sometimes the case, when you install Linux on a new PC, you have to install the right drivers to use your WiFi and other peripheral devices. In this guide I'll show how to install the RTL-8821-CE WiFi drivers.

## Prerequisites

The instructions on this guide were run on a Manjaro XCFE 21.2.6.

## Enable AUR and Install yay

The WiFi drivers you'll need are hosted in the [Arch User Repository (AUR)](https://aur.archlinux.org/). Before installing any packages, you'll have to enable it on your PC. To do so, install the `yay` AUR helper package by running the following command in the terminal:

```bash
sudo pacman -S git
git clone https://aur.archlinux.org/yay.git
cd yay
makepkg -si
```

## Install Kernel Headers

The first thing yuo'll need to install are your kernel headers. To check which version of the kernel you're using, run the following command in the terminal:

```bash
uname -r
```

The output should look similar to the following:

```text
5.15.41-1-MANJARO
```

The first two values are your major kernel version. For example, in the output above the kernel version is 5.15.

To install the kernel headers, run the following command in the terminal:

```bash
sudo pacman -S linux-headers
```

Make sure that the kernel version that's installed is the same as the one output after running the `uname -r` command. 

## Install RTL8821CE drivers

Once you've installed the headers, it's time to install the WiFi drivers. Use `yay` to run the following command in the terminal:

```bash
yay -S rtl8821ce-dkms-git
```

After some time, the packages should be installed on your device. Reboot your PC and you should now be able to connect to the internet wirelessly