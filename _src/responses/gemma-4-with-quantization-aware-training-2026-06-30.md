---
title: "Gemma 4 with quantization-aware training"
targeturl: https://blog.google/innovation-and-ai/technology/developers-tools/quantization-aware-training-gemma-4/
response_type: reshare
dt_published: "2026-06-30 21:48 -05:00"
dt_updated: "2026-06-30 21:48 -05:00"
tags: ["google","ai","gemma","quantization","llm"]
---

> Since releasing [Gemma 4](https://blog.google/innovation-and-ai/technology/developers-tools/gemma-4/) two months ago, we've been continuously working to expand its capabilities. First, we introduced [Multi-Token Prediction](https://blog.google/innovation-and-ai/technology/developers-tools/multi-token-prediction-gemma-4/) (MTP) to accelerate inference, and just a couple of days ago, we released [a 12B model](https://blog.google/innovation-and-ai/technology/developers-tools/introducing-gemma-4-12b/) to bridge the gap between our E4B and 26B MOE models.  
> <br>
> Today, we are releasing new checkpoints optimized with Quantization-Aware Training (QAT) to make Gemma 4 even more efficient, so you can run models locally on everyday edge devices and consumer GPUs.  
> <br>
> By simulating quantization during training, QAT minimizes quality loss when the model is compressed. This release includes QAT checkpoints for the popular Q4_0 quantization format as well as a novel quantization format specialized for mobile use cases. Using this mobile format, we’ve reduced the memory footprint of Gemma 4 E2B to 1GB. Together, these dramatically reduce memory requirements while preserving the capabilities and quality you expect from Gemma 4.