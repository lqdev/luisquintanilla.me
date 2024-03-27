---
title: "One-step Diffusion with Distribution Matching Distillation"
targeturl: https://tianweiy.github.io/dmd/
response_type: reshare
dt_published: "2024-03-26 21:44"
dt_updated: "2024-03-26 21:44 -05:00"
tags: ["ai","genai","diffusion","dmd","stablediffusion","dalle"]
---

> **Our one-step generator achieves comparable image quality with StableDiffusion v1.5 while being 30x faster.**  
> <br>
> Diffusion models are known to approximate the score function of the distribution they are trained on. In other words, an unrealistic synthetic image can be directed toward higher probability density region through the denoising process (see SDS). Our core idea is training two diffusion models to estimate not only the score function of the target real distribution, but also that of the fake distribution. We construct a gradient update to our generator as the difference between the two scores, essentially nudging the generated images toward higher realism as well as lower fakeness (see VSD). Our method is similar to GANs in that a critic is jointly trained with the generator to minimize a divergence between the real and fake distributions, but differs in that our training does not play an adversarial game that may cause training instability, and our critic can fully leverage the weights of a pretrained diffusion model. Combined with a simple regression loss to match the output of the multi-step diffusion model, our method outperforms all published few-step diffusion approaches, reaching 2.62 FID on ImageNet 64x64 and 11.49 FID on zero-shot COCO-30k, comparable to Stable Diffusion but orders of magnitude faster. Utilizing FP16 inference, our model generates images at 20 FPS on modern hardware. 

[Paper](https://arxiv.org/abs/2311.18828)