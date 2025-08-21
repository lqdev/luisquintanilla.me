---
title: "OnnxStream - Stable Diffusion XL 1.0 Base on a Raspberry Pi Zero 2"
targeturl: https://github.com/vitoplantamura/OnnxStream/
response_type: bookmark
dt_published: "2023-12-11 20:11"
dt_updated: "2023-12-11 20:11 -05:00"
tags: ["ai","onnx"]
---

> Generally major machine learning frameworks and libraries are focused on minimizing inference latency and/or maximizing throughput, all of which at the cost of RAM usage. So I decided to write a super small and hackable inference library specifically focused on minimizing memory consumption: OnnxStream.

> OnnxStream is based on the idea of decoupling the inference engine from the component responsible of providing the model weights, which is a class derived from WeightsProvider. A WeightsProvider specialization can implement any type of loading, caching and prefetching of the model parameters. For example a custom WeightsProvider can decide to download its data from an HTTP server directly, without loading or writing anything to disk (hence the word "Stream" in "OnnxStream"). Three default WeightsProviders are available: DiskNoCache, DiskPrefetch and Ram.

> OnnxStream can consume even 55x less memory than OnnxRuntime with only a 50% to 200% increase in latency (on CPU, with a good SSD, with reference to the SD 1.5's UNET - see the Performance section below).