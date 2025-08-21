---
title: "LLM training in simple, raw C/CUDA "
targeturl: https://github.com/karpathy/llm.c
response_type: reshare
dt_published: "2024-04-09 22:15 -05:00"
dt_updated: "2024-04-09 22:15 -05:00"
tags: ["llm","gpt","c","programming","learning","tutorial"]
---

> LLM training in simple, pure C/CUDA. There is no need for 245MB of PyTorch or 107MB of cPython. For example, training GPT-2 (CPU, fp32) is ~1,000 lines of clean code in a single file. It compiles and runs instantly, and exactly matches the PyTorch reference implementation. I chose GPT-2 as the first working example because it is the grand-daddy of LLMs, the first time the modern stack was put together.