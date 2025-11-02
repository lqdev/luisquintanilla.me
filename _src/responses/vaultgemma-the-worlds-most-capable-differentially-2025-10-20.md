---
title: "VaultGemma: The world's most capable differentially private LLM"
targeturl: https://research.google/blog/vaultgemma-the-worlds-most-capable-differentially-private-llm/
response_type: reshare
dt_published: "2025-10-20 15:35 -05:00"
dt_updated: "2025-10-20 15:35 -05:00"
tags: ["privacy","ai","llm","gemma","google","differentialprivacy","research"]
---

> As AI becomes more integrated into our lives, building it with privacy at its core is a critical frontier for the field. Differential privacy (DP) offers a mathematically sound solution by adding calibrated noise to prevent memorization. However, applying DP to LLMs introduces trade-offs. Understanding these trade-offs is crucial. Applying DP noise alters traditional scaling laws — rules describing performance dynamics — by reducing training stability (the model's ability to learn consistently without experiencing catastrophic events like loss spikes or divergence) and significantly increasing batch size (a collection of training examples sent to the model simultaneously for processing) and computation costs.  
<br>
> Our new research, “Scaling Laws for Differentially Private Language Models”, conducted in partnership with Google DeepMind, establishes laws that accurately model these intricacies, providing a complete picture of the compute-privacy-utility trade-offs. Guided by this research, we’re excited to introduce VaultGemma, the largest (1B-parameters), open model trained from scratch with differential privacy.

[Scaling Laws for Differentially Private Language Models Paper](https://arxiv.org/abs/2501.18914)