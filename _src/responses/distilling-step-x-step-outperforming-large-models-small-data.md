---
title: "Distilling step-by-step: Outperforming larger language models with less training data and smaller model sizes"
targeturl: https://blog.research.google/2023/09/distilling-step-by-step-outperforming.html 
response_type: bookmark
dt_published: "2023-09-25 20:45"
dt_updated: "2023-09-25 20:45 -05:00"
tags: ["ai","llm","slm","optimization"]
---

> In [“Distilling Step-by-Step! Outperforming Larger Language Models with Less Training Data and Smaller Model Sizes”](https://arxiv.org/abs/2305.02301), presented at ACL2023, we set out to tackle this trade-off between model size and training data collection cost. We introduce distilling step-by-step, a new simple mechanism that allows us to train smaller task-specific models with much less training data than required by standard fine-tuning or distillation approaches that outperform few-shot prompted LLMs’ performance. We demonstrate that the distilling step-by-step mechanism enables a 770M parameter T5 model to outperform the few-shot prompted 540B PaLM model using only 80% of examples in a benchmark dataset, which demonstrates a more than 700x model size reduction with much less training data required by standard approaches. 