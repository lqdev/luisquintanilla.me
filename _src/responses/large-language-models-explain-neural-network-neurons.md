---
title: "Language models can explain neurons in language models"
targeturl: https://openaipublic.blob.core.windows.net/neuron-explainer/paper/index.html
response_type: bookmark
dt_published: "2023-05-09 20:59"
dt_updated: "2023-05-09 20:59 -05:00"
tags: ["ai","llm","deeplearning"]
---

> This paper applies automation to the problem of scaling an interpretability technique to all the neurons in a large language model. Our hope is that building on this approach of automating interpretability will enable us to comprehensively audit the safety of models before deployment.

> Our technique seeks to explain what patterns in text cause a neuron to activate. It consists of three steps:
> 1. Explain the neuron's activations using GPT-4
> 2. Simulate activations using GPT-4, conditioning on the explanation
> 3. Score the explanation by comparing the simulated and real activations
