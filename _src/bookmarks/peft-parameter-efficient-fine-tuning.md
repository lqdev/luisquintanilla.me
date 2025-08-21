---
title: "PEFT: Parameter-Efficient Fine-Tuning of Billion-Scale Models on Low-Resource Hardware"
targeturl: https://huggingface.co/blog/peft 
response_type: bookmark
dt_published: "2023-04-20 21:32 -05:00"
dt_updated: "2023-04-20 21:32 -05:00"
tags: ["ai","finetuning"]
---

> ...as models get larger and larger, full fine-tuning becomes infeasible to train on consumer hardware. In addition, storing and deploying fine-tuned models independently for each downstream task becomes very expensive, because fine-tuned models are the same size as the original pretrained model. Parameter-Efficient Fine-tuning (PEFT) approaches are meant to address both problems!

> PEFT approaches only fine-tune a small number of (extra) model parameters while freezing most parameters of the pretrained LLMs, thereby greatly decreasing the computational and storage costs. This also overcomes the issues of catastrophic forgetting, a behaviour observed during the full finetuning of LLMs. PEFT approaches have also shown to be better than fine-tuning in the low-data regimes and generalize better to out-of-domain scenarios. It can be applied to various modalities, e.g., image classification and stable diffusion dreambooth.