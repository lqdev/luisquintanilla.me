---
title: "Reproducing GPT-2 (124M) in llm.c in 90 minutes for $20"
targeturl: https://github.com/karpathy/llm.c/discussions/481
response_type: reshare
dt_published: "2024-05-28 20:33"
dt_updated: "2024-05-28 20:33 -05:00"
tags: ["ai","llm","gpt","gpt2","llmc","c","slm"]
---

> ...the TLDR is that we're training a 12-layer GPT-2 (124M), from scratch, on 10B tokens of FineWeb, with max sequence length of 1024 tokens. 

> The 124M model is the smallest model in the GPT-2 series released by OpenAI in 2019, and is actually quite accessible today, even for the GPU poor. With llm.c, which is quite efficient at up to ~60% model flops utilization, reproducing this model on one 8X A100 80GB SXM node takes ~90 minutes. For example, on Lambda this node goes for ~$14/hr, so the total cost of reproducing this model today is about $20. You can train the model with a single GPU too, it would just take proportionally longer (e.g. ~4-24 hours depending on the GPU).