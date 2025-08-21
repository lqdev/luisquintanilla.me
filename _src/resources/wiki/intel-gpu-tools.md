---
post_type: "wiki" 
title: "Intel GPU Tools - Linux"
last_updated_date: "12/11/2022 18:05 -05:00"
tags: linux, intel, gpu, manjaro, performances
---

## Overview

This article talks about Intel GPU tools for Linux to monitor usage of integrated Intel GPUs 

## When to use

The scenarios in which I've used this tool is to confirm that my browser is leveraging hardware acceleration. By default, Firefox doesn't enable hardware acceleration on Linux. As a result, everything runs on the CPU causing unnecessary load and faster battery drain. To alleviate that, you can enable hardware acceleration and make sure that the GPU is being used by running tools like `intel_gpu_top`. 

## Install

To install the tools, run the following command

```bash
sudo pacman -S intel-gpu-tools
```

## Run intel_gpu_top

To view GPU activity, run the following command:

```bash
sudo intel_gpu_top
```

This will display process graphs of GPU usage.  

## References

- [Intel Grapics - Arch Wiki](https://wiki.archlinux.org/title/Intel_graphics)
- [Intel GPU Tools](https://archlinux.org/packages/community/x86_64/intel-gpu-tools/)
- [Firefox hardware acceleration](https://support.mozilla.org/en-US/kb/upgrade-graphics-drivers-use-hardware-acceleration)
- [Firefox Performance Settings](https://support.mozilla.org/en-US/kb/performance-settings)