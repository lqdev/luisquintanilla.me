---
title: "RL Learning with LoRA: A Diverse Deep Dive"
targeturl: https://kalomaze.bearblog.dev/rl-lora-ddd/
response_type: reshare
dt_published: "2025-11-12 01:45 -05:00"
dt_updated: "2025-11-12 01:45 -05:00"
tags: ["lora","ai","finetuning","reinforcementlearning","rl"]
---

> In this post, I'll be covering LoRA training and its recent incorporation into [prime-rl](https://github.com/PrimeIntellect-ai/prime-rl) for both SFT and RL finetuning, including practical implementation details & experimental training results for some of our RL environments.

> prime-rl has full support for LoRA. We plan on continuing improving it with better LoRA algorithms and more efficient implementations. We also are working on MoE support, as well as the ability to train multiple LoRA adapters at the same time in preparation for our upcoming Reinforcement Fine-tuning API launch.