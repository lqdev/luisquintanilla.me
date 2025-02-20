---
title: "The Ultra-Scale Playbook: Training LLMs on GPU Clusters"
targeturl: https://huggingface.co/spaces/nanotron/ultrascale-playbook
response_type: reshare
dt_published: "2025-02-19 22:03"
dt_updated: "2025-02-19 22:03 -05:00"
tags: ["ai","llm","training"]
---

Gold. 

> All the techniques we'll cover in this book tackle one or several of the following three key challenges, which we'll keep bumping into throughout the book:  
> <hr>
>   1. **Memory Usage**: it's a hard limitation - if a training step doesn't fit in memory, training cannot proceed
>   2. **Compute Efficiency**: we want our hardware to spend most time computing, so we need to reduce time spent on data transfers or waiting for other GPUs to perform work.
>   3. **Communication overhead**: we want to minimize communication overhead as it keeps GPUs idle. To archieve this we will try to make best use of intra-node (fast) and inter-node (slower) bandwidths as well as overlap communication with compute as much as possible.  
> <hr>
> In many places we'll see that we can trade one of these (computation, communication, memory) for another (e.g. recomputation or Tensor Parallelism). Finding the right balance is key to scaling training.

![Ultra-Scale Playbook Cheatsheet](https://nanotron-ultrascale-playbook.static.hf.space/dist/assets/images/ultra-cheatsheet.svg)