---
title: "Sampling for Text Generation"
targeturl: https://huyenchip.com//2024/01/16/sampling.html
response_type: bookmark
dt_published: "2024-01-17 20:32"
dt_updated: "2024-01-17 20:32 -05:00"
tags: ["ai","generativeai","statistics","llm"]
---

> ML models are probabilistic. Imagine that you want to know what’s the best cuisine in the world. If you ask someone this question twice, a minute apart, their answers both times should be the same. If you ask a model the same question twice, its answer can change.

> This probabilistic nature makes AI great for creative tasks.

> However, this probabilistic nature also causes inconsistency and hallucinations. It’s fatal for tasks that depend on factuality. Recently, I went over 3 months’ worth of customer support requests of an AI startup I advise and found that ⅕ of the questions are because users don’t understand or don’t know how to work with this probabilistic nature.

> To understand why AI’s responses are probabilistic, we need to understand how models generate responses, a process known as sampling (or decoding). This post consists of 3 parts.
> <br>
> 
>     1. Sampling: sampling strategies and sampling variables including temperature, top-k, and top-p.
>     2. Test time sampling: sampling multiple outputs to help improve a model’s performance.
>     3. Structured outputs: how to get models to generate outputs in a certain format.
