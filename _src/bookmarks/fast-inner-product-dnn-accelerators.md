---
title: "Fast Inner-Product Algorithms and Architectures for Deep Neural Network Accelerators"
targeturl: https://arxiv.org/abs/2311.12224
response_type: bookmark
dt_published: "2024-03-17 20:47 -05:00"
dt_updated: "2024-03-17 20:47 -05:00"
tags: ["ai","math","acceleration","neuralnetworks","dnn"]
---

> We introduce a new algorithm called the Free-pipeline Fast Inner Product (FFIP) and its hardware architecture that improve an under-explored fast inner-product algorithm (FIP) proposed by Winograd in 1968. Unlike the unrelated Winograd minimal filtering algorithms for convolutional layers, FIP is applicable to all machine learning (ML) model layers that can mainly decompose to matrix multiplication, including fully-connected, convolutional, recurrent, and attention/transformer layers. We implement FIP for the first time in an ML accelerator then present our FFIP algorithm and generalized architecture which inherently improve FIP's clock frequency and, as a consequence, throughput for a similar hardware cost. Finally, we contribute ML-specific optimizations for the FIP and FFIP algorithms and architectures. We show that FFIP can be seamlessly incorporated into traditional fixed-point systolic array ML accelerators to achieve the same throughput with half the number of multiply-accumulate (MAC) units, or it can double the maximum systolic array size that can fit onto devices with a fixed hardware budget. Our FFIP implementation for non-sparse ML models with 8 to 16-bit fixed-point inputs achieves higher throughput and compute efficiency than the best-in-class prior solutions on the same type of compute platform. 

[GitHub](https://github.com/trevorpogue/algebraic-nnhw)
