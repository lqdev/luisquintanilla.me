---
title: "LoRA Without Regret"
targeturl: https://thinkingmachines.ai/blog/lora/
response_type: bookmark
dt_published: "2025-10-06 08:32 -05:00"
dt_updated: "2025-10-06 08:32 -05:00"
tags: ["ai","llm","finetuning","research","thinkingmachines","lora"]
---

> We find that:  
> <br>
> - For supervised fine-tuning on small-to-medium-sized instruction-tuning and reasoning datasets, LoRA performs the same as full fine-tuning.
> - For datasets that exceed LoRA capacity, LoRA underperforms FullFT. Rather than the loss reaching a distinct floor that it can’t go below, LoRA results in worse training efficiency that depends on the relationship between model capacity to dataset size.
> - In some scenarios, LoRA is less tolerant of large batch sizes than full fine-tuning — it pays a larger penalty in loss as batch size increases beyond some point. This penalty is not mitigated by increasing the LoRA rank; it is a property of the product-of-matrices parametrization, which has different training dynamics than optimizing the original weight matrix.
> - Even in small data settings, LoRA performs better when applied to all weight matrices, especially MLP and MoE layers. Attention-only LoRA underperforms even when we match the number of trainable parameters by using higher rank for attention-only LoRA.
> - LoRA performs equivalently to FullFT for reinforcement learning even with small ranks. We find that RL requires very low capacity, a result we anticipated based on information-theoretical arguments."