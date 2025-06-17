---
title: "Self-Adapting Language Models (SEAL)"
targeturl: https://jyopari.github.io/posts/seal
response_type: reshare
dt_published: "2025-06-16 20:33"
dt_updated: "2025-06-16 20:33 -05:00"
tags: ["agent","ai","llm","mit","research"]
---

> Large language models (LLMs) are powerful but static; they lack mechanisms to adapt their weights in response to new tasks, knowledge, or examples. **We introduce Self-Adapting LLMs (SEAL) 🦭, a framework that enables LLMs to self-adapt by generating their own finetuning data and update directives.** Given a new input, the model produces a self-edit — a generation that may restructure the information in different ways, specify optimization hyperparameters, or invoke tools for data augmentation and gradient-based updates. Through supervised finetuning (SFT), these self-edits result in persistent weight updates, enabling lasting adaptation. To train the model to produce effective self-edits, we use a reinforcement learning loop, using the downstream performance of the updated model as the reward signal. Unlike prior approaches that rely on separate adaptation modules or auxiliary networks, SEAL directly uses the model's generation to parameterize and control its own adaptation process. Experiments on knowledge incorporation and few-shot generalization show that SEAL is a promising step toward language models capable of self-directed adaptation in response to new data.

> We demonstrate SEAL in two domains: (1) **Knowledge Incorporation**, where the model integrates new factual information by generating logical implications as synthetic data, and (2) **Few-Shot Learning**, where the model autonomously selects data augmentations and training hyperparameters to adapt to new abstract reasoning tasks.

- [Code](https://github.com/Continual-Intelligence)
- [Paper](https://arxiv.org/abs/2506.10943)