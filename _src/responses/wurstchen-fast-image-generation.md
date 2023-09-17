---
title: "Introducing Würstchen: Fast Diffusion for Image Generation "
targeturl: https://huggingface.co/blog/wuerstchen
response_type: bookmark
dt_published: "2023-09-17 12:15"
dt_updated: "2023-09-17 12:15 -05:00"
tags: ["ai","generativeai","imagegeneration","huggingface","diffusion","ml"]
---

> Würstchen is a diffusion model, whose text-conditional component works in a highly compressed latent space of images. Why is this important? Compressing data can reduce computational costs for both training and inference by orders of magnitude. Training on 1024×1024 images is way more expensive than training on 32×32. Usually, other works make use of a relatively small compression, in the range of 4x - 8x spatial compression. Würstchen takes this to an extreme. Through its novel design, it achieves a 42x spatial compression! This had never been seen before, because common methods fail to faithfully reconstruct detailed images after 16x spatial compression. Würstchen employs a two-stage compression, what we call Stage A and Stage B. Stage A is a VQGAN, and Stage B is a Diffusion Autoencoder (more details can be found in the  paper). Together Stage A and B are called the Decoder, because they decode the compressed images back into pixel space. A third model, Stage C, is learned in that highly compressed latent space. This training requires fractions of the compute used for current top-performing models, while also allowing cheaper and faster inference. We refer to Stage C as the Prior.