---
title: "Introducing AutoRound"
targeturl: https://huggingface.co/blog/autoround
response_type: bookmark
dt_published: "2025-05-09 20:45"
dt_updated: "2025-05-09 20:45 -05:00"
tags: ["quantization","intel","ai"]
---

> As large language models (LLMs) and vision-language models (VLMs) continue to grow in size and complexity, deploying them efficiently becomes increasingly challenging. Quantization offers a solution by reducing model size and inference latency. Intel's AutoRound emerges as a cutting-edge quantization tool that balances accuracy, efficiency, and compatibility.  
> <br>
> AutoRound is a weight-only post-training quantization (PTQ) method developed by Intel. It uses signed gradient descent to jointly optimize weight rounding and clipping ranges, enabling accurate low-bit quantization (e.g., INT2 - INT8) with minimal accuracy loss in most scenarios. For example, at INT2, it outperforms popular baselines by up to 2.1x higher in relative accuracy.

- [Paper](https://arxiv.org/abs/2309.05516)
- [Repo](https://github.com/intel/auto-round)