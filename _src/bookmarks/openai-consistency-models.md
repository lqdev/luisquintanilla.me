---
title: "Consistency Models"
targeturl: https://github.com/openai/consistency_models
response_type: bookmark
dt_published: "2023-04-14 14:22 -05:00"
dt_updated: "2023-04-14 14:22 -05:00"
tags: [openai,ai]
---

Paper: https://arxiv.org/abs/2303.01469

>  Diffusion models have made significant breakthroughs in image, audio, and video generation, but they depend on an iterative generation process that causes slow sampling speed and caps their potential for real-time applications. To overcome this limitation, we propose consistency models, a new family of generative models that achieve **high sample quality without adversarial training**. They **support fast one-step generation** by design, while still allowing for few-step sampling to trade compute for sample quality. They also **support zero-shot data editing**, like image inpainting, colorization, and super-resolution, without requiring explicit training on these tasks. Consistency models can be trained either as a way to distill pre-trained diffusion models, or as standalone generative models. Through extensive experiments, we demonstrate that they outperform existing distillation techniques for diffusion models in one- and few-step generation. For example, we achieve the new state-of-the-art FID of 3.55 on CIFAR-10 and 6.20 on ImageNet 64x64 for one-step generation. When trained as standalone generative models, consistency models also outperform single-step, non-adversarial generative models on standard benchmarks like CIFAR-10, ImageNet 64x64 and LSUN 256x256. 