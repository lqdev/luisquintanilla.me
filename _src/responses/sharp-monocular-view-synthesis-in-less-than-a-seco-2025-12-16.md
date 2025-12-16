---
title: "Sharp Monocular View Synthesis in Less Than a Second"
targeturl: https://apple.github.io/ml-sharp/
response_type: reshare
dt_published: "2025-12-16 09:10 -05:00"
dt_updated: "2025-12-16 09:10 -05:00"
tags: ["ai","ml","apple","computervision"]
---

> We present SHARP, an approach to photorealistic view synthesis from a single image. Given a single photograph, SHARP regresses the parameters of a 3D Gaussian representation of the depicted scene. This is done in less than a second on a standard GPU via a single feedforward pass through a neural network. The 3D Gaussian representation produced by SHARP can then be rendered in real time, yielding high-resolution photorealistic images for nearby views. The representation is metric, with absolute scale, supporting metric camera movements. Experimental results demonstrate that SHARP delivers robust zero-shot generalization across datasets. It sets a new state of the art on multiple datasets, reducing LPIPS by 25–34% and DISTS by 21–43% versus the best prior model, while lowering the synthesis time by three orders of magnitude.

- [Paper](https://arxiv.org/abs/2512.10685)
- [Code](https://github.com/apple/ml-sharp)