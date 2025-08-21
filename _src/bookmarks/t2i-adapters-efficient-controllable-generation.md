---
title: "Efficient Controllable Generation for SDXL with T2I-Adapters "
targeturl: https://huggingface.co/blog/t2i-sdxl-adapters 
response_type: bookmark
dt_published: "2023-09-17 12:11 -05:00"
dt_updated: "2023-09-17 12:11 -05:00"
tags: ["ai","generativeai","imagegeneration","deeplearning","ml","huggingface"]
---


> [T2I-Adapter](https://huggingface.co/papers/2302.08453) is an efficient plug-and-play model that provides extra guidance to pre-trained text-to-image models while freezing the original large text-to-image models. T2I-Adapter aligns internal knowledge in T2I models with external control signals. We can train various adapters according to different conditions and achieve rich control and editing effects.

> Over the past few weeks, the Diffusers team and the T2I-Adapter authors have been collaborating to bring the support of T2I-Adapters for Stable Diffusion XL (SDXL) in diffusers. In this blog post, we share our findings from training T2I-Adapters on SDXL from scratch, some appealing results, and, of course, the T2I-Adapter checkpoints on various conditionings (sketch, canny, lineart, depth, and openpose)!