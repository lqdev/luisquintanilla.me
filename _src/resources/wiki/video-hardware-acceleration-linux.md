---
post_type: "wiki" 
title: "Hardware Video Acceleration - Linux"
last_updated_date: "12/11/2022 17:36 -05:00"
tags: performance, wiki, hardware, video, hardwareacceleration, linux, arch, manjaro
---

## Overview

This article talks about using hardware video acceleration so the video card encodes/decodes video offloading the CPU and saving power.  

## Install intel-media-driver

To install the Intel media driver

```bash
sudo pacman -S intel-media-driver
```

## Enable acceleration MPV

For mpv, make sure that decoding and video acceleration is enabled. To do so:

1. Create or edit the user config file in *~/.config/mpv/mpv.conf* with the following settings:

    ```bash
    vo=gpu
    hwdec=auto
    ```

1. Reboot your PC.

## Verification

Use `intel_gpu_top` and make sure that when you play video using tools like VLC or MPV, the video bar is active. 

## References

- [Hardware Video Acceleration - Intel GPU](https://wiki.archlinux.org/title/Hardware_video_acceleration)
- [Intel GPU Tools](/resources/wiki/intel-gpu-tools)
- [Sample MPV Conf](https://github.com/mpv-player/mpv/blob/master/etc/mpv.conf)
- [MPV Configuration Files](https://mpv.io/manual/master/#configuration-files)