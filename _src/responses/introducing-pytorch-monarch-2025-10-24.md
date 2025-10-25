---
title: "Introducing PyTorch Monarch"
targeturl: https://pytorch.org/blog/introducing-pytorch-monarch/
response_type: reshare
dt_published: "2025-10-24 21:48 -05:00"
dt_updated: "2025-10-24 21:48 -05:00"
tags: ["pytorch","distributedsystems","python","ai","machinelearning","deeplearning","ml","hpc","actors"]
---

> We’re excited to introduce Monarch, a distributed programming framework that brings the simplicity of single-machine PyTorch to entire clusters.

> Monarch lets you program distributed systems the way you’d program a single machine, hiding the complexity of distributed computing:  
> <br>
> 1. **Program clusters like arrays**. Monarch organizes hosts, processes, and actors into scalable meshes that you can manipulate directly. You can operate on entire meshes (or slices of them) with simple APIs—Monarch handles the distribution and vectorization automatically, so you can think in terms of what you want to compute, not where the code runs.
> 2. **Progressive fault handling**. With Monarch, you write your code as if nothing fails. When something does fail, Monarch fails fast by default—stopping the whole program, just like an uncaught exception in a simple local script. Later, you can progressively add fine-grained fault handling exactly where you need it, catching and recovering from failures just like you’d catch exceptions.
> 3. **Separate control from data**. Monarch splits control plane (messaging) from data plane (RDMA transfers), enabling direct GPU-to-GPU memory transfers across your cluster. Monarch lets you send commands through one path, while moving data through another, optimized for what each does best.
> 4. **Distributed tensors that feel local**. Monarch integrates seamlessly with PyTorch to provide tensors that are sharded across clusters of GPUs. Monarch tensor operations look local but are executed across distributed large clusters, with Monarch handling the complexity of coordinating across thousands of GPUs."