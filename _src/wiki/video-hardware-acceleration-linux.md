---
post_type: "wiki" 
title: "Hardware Video Acceleration - Linux"
last_updated_date: "12/11/2022 17:36"
tags: performance, wiki, hardware, video, hardwareacceleration, linux, arch, manjaro
---

## Overview

This article talks about using hardware video acceleration so the video card encodes/decodes video offloading the CPU and saving power.  

## Install intel-media-driver

To install the Intel media driver

```bash
sudo pacman -S intel-media-driver
```

## Verification

Use `intel_gpu_top` and make sure that when you play video using  tools like VLC, the video bar is active. 

## References

- [Hardware Video Acceleration - Intel GPU](https://wiki.archlinux.org/title/Hardware_video_acceleration)
- [Intel GPU Tools](./intel-gpu-tools)
