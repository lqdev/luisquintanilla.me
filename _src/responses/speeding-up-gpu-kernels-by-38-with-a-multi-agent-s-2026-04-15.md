---
title: "Speeding up GPU kernels by 38% with a multi-agent system"
targeturl: https://cursor.com/blog/multi-agent-kernels
response_type: reshare
dt_published: "2026-04-15 22:17 -05:00"
dt_updated: "2026-04-15 22:17 -05:00"
tags: ["ai","nvidia","cursor","agents","kernel","gpu"]
---

> Recently, we began collaborating with NVIDIA on a new challenge: applying the multi-agent harness to optimize CUDA kernels. These are difficult technical problems with important real-world consequences: CUDA kernels are the core software that supports AI model training and inference on NVIDIA GPUs. Faster kernels mean better GPU utilization, reduced energy consumption, lower latency, and reduced cost per token—allowing providers to serve bigger, more capable models to more users at once.  
> <br>
> Our multi-agent harness operated autonomously for three weeks across 235 problems. The system achieved a 38% geomean speedup by building and optimizing Blackwell GPU kernels from scratch, all the way down to the assembly level.  
> <br>
> These levels of performance improvement are typically only found through months or years of work from highly experienced kernel engineers. The multi-agent system accomplished it in weeks, addressing a long-tail of kernel problems that had been impractical with existing approaches.