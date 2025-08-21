---
title: "Griffin: Mixing Gated Linear Recurrences with Local Attention for Efficient Language Models"
targeturl: https://arxiv.org/abs/2402.19427
response_type: bookmark
dt_published: "2024-04-10 22:02 -05:00"
dt_updated: "2024-04-10 22:02 -05:00"
tags: ["griffin","ai","research","architecture","rnn","attention","transformers","llm"]
---

> Recurrent neural networks (RNNs) have fast inference and scale efficiently on long sequences, but they are difficult to train and hard to scale. We propose Hawk, an RNN with gated linear recurrences, and Griffin, a hybrid model that mixes gated linear recurrences with local attention. Hawk exceeds the reported performance of Mamba on downstream tasks, while Griffin matches the performance of Llama-2 despite being trained on over 6 times fewer tokens. We also show that Griffin can extrapolate on sequences significantly longer than those seen during training. Our models match the hardware efficiency of Transformers during training, and during inference they have lower latency and significantly higher throughput. We scale Griffin up to 14B parameters, and explain how to shard our models for efficient distributed training. 
