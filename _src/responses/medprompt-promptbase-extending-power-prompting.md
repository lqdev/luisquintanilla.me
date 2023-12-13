---
title: "Steering at the Frontier: Extending the Power of Prompting "
targeturl: https://www.microsoft.com/research/blog/steering-at-the-frontier-extending-the-power-of-prompting/
response_type: bookmark
dt_published: "2023-12-12 19:14"
dt_updated: "2023-12-12 19:14 -05:00"
tags: ["ai","promptengineering"]
---

> ...steering GPT-4 with a modified version of Medprompt achieves the highest score ever achieved on the complete MMLU.

> To achieve a new SoTA on MMLU, we extended Medprompt to Medprompt+ by adding a simpler prompting method and formulating a policy for deriving a final answer by integrating outputs from both the base Medprompt strategy and the simple prompts. The synthesis of a final answer is guided by a control strategy governed by GPT-4 and inferred confidences of candidate answers. 

> While systematic prompt engineering can yield maximal performance, we continue to explore the out-of-the-box performance of frontier models with simple prompts. It’s important to keep an eye on the native power of GPT-4 and how we can steer the model with zero- or few-shot prompting strategies.