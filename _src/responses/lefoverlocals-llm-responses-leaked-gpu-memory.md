---
title: "LeftoverLocals: Listening to LLM responses through leaked GPU local memory"
targeturl: https://blog.trailofbits.com/2024/01/16/leftoverlocals-listening-to-llm-responses-through-leaked-gpu-local-memory/
response_type: reshare
dt_published: "2024-01-16 20:16 -05:00"
dt_updated: "2024-01-16 20:16 -05:00"
tags: ["security","llm","ai"]
---

> We are disclosing LeftoverLocals: a vulnerability that allows recovery of data from GPU local memory created by another process on Apple, Qualcomm, AMD, and Imagination GPUs. LeftoverLocals impacts the security posture of GPU applications as a whole, with particular significance to LLMs and ML models run on impacted GPU platforms. By recovering local memory—an optimized GPU memory region—we were able to build a PoC where an attacker can listen into another user’s interactive LLM session (e.g., llama.cpp) across process or container boundaries
