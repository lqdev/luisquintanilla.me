---
title: "Introducing DiffusionGemma"
targeturl: https://blog.google/innovation-and-ai/technology/developers-tools/diffusion-gemma-faster-text-generation/
response_type: reshare
dt_published: "2026-06-30 22:35 -05:00"
dt_updated: "2026-06-30 22:35 -05:00"
tags: ["google","ai","gemma","diffusion","llm"]
---

> Today, we’re introducing DiffusionGemma, an experimental open model that explores text diffusion, an exceptionally fast approach to text generation. Released under an Apache 2.0 license, this 26B Mixture of Experts (MoE) model moves beyond the sequential token-by-token processing of typical autoregressive Large Language Models (LLMs). Instead, it generates entire blocks of text simultaneously, delivering up to 4x faster text generation on GPUs.

> Built upon the industry-leading intelligence-per-parameter of our Gemma 4 family and cutting-edge [Gemini Diffusion research](https://deepmind.google/models/gemini-diffusion/), DiffusionGemma integrates a novel diffusion head designed to maximize generation speed. While autoregressive Gemma 4 models remain the standard for high-quality production outputs, DiffusionGemma is designed for researchers and developers exploring speed-critical, interactive local workflows such as in-line editing, rapid iteration, and generating non-linear text structures.