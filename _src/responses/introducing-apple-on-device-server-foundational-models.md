---
title: "Introducing Apple’s On-Device and Server Foundation Models"
targeturl: https://machinelearning.apple.com/research/introducing-apple-foundation-models
response_type: reshare
dt_published: "2024-06-11 20:26 -05:00"
dt_updated: "2024-06-11 20:26 -05:00"
tags: ["ai","apple","slm","edge","wwdc"]
---

> Apple Intelligence is comprised of multiple highly-capable generative models that are specialized for our users’ everyday tasks, and can adapt on the fly for their current activity. The foundation models built into Apple Intelligence have been fine-tuned for user experiences such as writing and refining text, prioritizing and summarizing notifications, creating playful images for conversations with family and friends, and taking in-app actions to simplify interactions across apps.

> In the following overview, we will detail how two of these models — a ~3 billion parameter on-device language model, and a larger server-based language model available with Private Cloud Compute and running on Apple silicon servers — have been built and adapted to perform specialized tasks efficiently, accurately, and responsibly. These two foundation models are part of a larger family of generative models created by Apple to support users and developers; this includes a coding model to build intelligence into Xcode, as well as a diffusion model to help users express themselves visually, for example, in the Messages app.

## Pre-training

> Our foundation models are trained on Apple's AXLearn framework, an open-source project we released in 2023. It builds on top of JAX and XLA, and allows us to train the models with high efficiency and scalability on various training hardware and cloud platforms, including TPUs and both cloud and on-premise GPUs. We used a combination of data parallelism, tensor parallelism, sequence parallelism, and Fully Sharded Data Parallel (FSDP) to scale training along multiple dimensions such as data, model, and sequence length.

## Post-training

> We find that data quality is essential to model success, so we utilize a hybrid data strategy in our training pipeline, incorporating both human-annotated and synthetic data, and conduct thorough data curation and filtering procedures. We have developed two novel algorithms in post-training: (1) a rejection sampling fine-tuning algorithm with teacher committee, and (2) a reinforcement learning from human feedback (RLHF) algorithm with mirror descent policy optimization and a leave-one-out advantage estimator. We find that these two algorithms lead to significant improvement in the model’s instruction-following quality.

## Optimization

> Both the on-device and server models use grouped-query-attention. We use shared input and output vocab embedding tables to reduce memory requirements and inference cost. These shared embedding tensors are mapped without duplications. The on-device model uses a vocab size of 49K, while the server model uses a vocab size of 100K, which includes additional language and technical tokens.

> For on-device inference, we use low-bit palletization, a critical optimization technique that achieves the necessary memory, power, and performance requirements. To maintain model quality, we developed a new framework using LoRA adapters that incorporates a mixed 2-bit and 4-bit configuration strategy — averaging 3.5 bits-per-weight — to achieve the same accuracy as the uncompressed models.

> Additionally, we use an interactive model latency and power analysis tool, Talaria, to better guide the bit rate selection for each operation. We also utilize activation quantization and embedding quantization, and have developed an approach to enable efficient Key-Value (KV) cache update on our neural engines.

## Model Adaptation

> Our foundation models are fine-tuned for users’ everyday activities, and can dynamically specialize themselves on-the-fly for the task at hand...

> We represent the values of the adapter parameters using 16 bits, and for the ~3 billion parameter on-device model, the parameters for a rank 16 adapter typically require 10s of megabytes. The adapter models can be dynamically loaded, temporarily cached in memory, and swapped — giving our foundation model the ability to specialize itself on the fly for the task at hand while efficiently managing memory and guaranteeing the operating system's responsiveness.

## Performance and Evaluation

> We compare our models with both open-source models (Phi-3, Gemma, Mistral, DBRX) and commercial models of comparable size (GPT-3.5-Turbo, GPT-4-Turbo)1. We find that our models are preferred by human graders over most comparable competitor models. On this benchmark, our on-device model, with ~3B parameters, outperforms larger models including Phi-3-mini, Mistral-7B, and Gemma-7B. Our server model compares favorably to DBRX-Instruct, Mixtral-8x22B, and GPT-3.5-Turbo while being highly efficient.

> To further evaluate our models, we use the Instruction-Following Eval (IFEval) benchmark to compare their instruction-following capabilities with models of comparable size. The results suggest that both our on-device and server model follow detailed instructions better than the open-source and commercial models of comparable size.