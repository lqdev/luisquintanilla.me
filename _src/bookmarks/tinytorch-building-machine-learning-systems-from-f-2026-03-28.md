---
title: "TinyTorch: Building Machine Learning Systems from First Principles"
targeturl: https://arxiv.org/abs/2601.19107
response_type: bookmark
dt_published: "2026-03-28 11:06 -05:00"
dt_updated: "2026-03-28 11:06 -05:00"
tags: ["ai","curriculum","deeplearning","course","research"]
---

> Machine learning education faces a fundamental gap: students learn algorithms without understanding the systems that execute them. They study gradient descent without measuring memory, attention mechanisms without analyzing O(N^2) scaling, optimizer theory without knowing why Adam requires 3x the memory of SGD. This "algorithm-systems divide" produces practitioners who can train models but cannot debug memory failures, optimize inference latency, or reason about deployment trade-offs--the very skills industry demands as "ML systems engineering." We present TinyTorch, a 20-module curriculum that closes this gap through "implementation-based systems pedagogy": students construct PyTorch's core components (tensors, autograd, optimizers, CNNs, transformers) in pure Python, building a complete framework where every operation they invoke is code they wrote. The design employs three patterns: "progressive disclosure" of complexity, "systems-first integration" of profiling from the first module, and "build-to-validate milestones" recreating 67 years of ML breakthroughs--from Perceptron (1958) through Transformers (2017) to MLPerf-style benchmarking. Requiring only 4GB RAM and no GPU, TinyTorch demonstrates that deep ML systems understanding is achievable without specialized hardware. The curriculum is available open-source at [this http URL](http://mlsysbook.ai/tinytorch).