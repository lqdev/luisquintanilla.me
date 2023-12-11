---
title: "Mixtral of experts"
targeturl: https://mistral.ai/news/mixtral-of-experts/
response_type: bookmark
dt_published: "2023-12-11 18:50"
dt_updated: "2023-12-11 18:50 -05:00"
tags: ["ai","llm","opensource"]
---

> Today, the team is proud to release Mixtral 8x7B, a high-quality sparse mixture of experts model (SMoE) with open weights. Licensed under Apache 2.0. Mixtral outperforms Llama 2 70B on most benchmarks with 6x faster inference. It is the strongest open-weight model with a permissive license and the best model overall regarding cost/performance trade-offs. In particular, it matches or outperforms GPT3.5 on most standard benchmarks.
> 
> Mixtral has the following capabilities.
> 
>   - It gracefully handles a context of 32k tokens.
>   - It handles English, French, Italian, German and Spanish.
>   - It shows strong performance in code generation.
>   - It can be finetuned into an instruction-following model that achieves a score of 8.3 on MT-Bench.

> Mixtral is a sparse mixture-of-experts network. It is a decoder-only model where the feedforward block picks from a set of 8 distinct groups of parameters. At every layer, for every token, a router network chooses two of these groups (the “experts”) to process the token and combine their output additively.
> 
> This technique increases the number of parameters of a model while controlling cost and latency, as the model only uses a fraction of the total set of parameters per token. Concretely, Mixtral has 46.7B total parameters but only uses 12.9B parameters per token. It, therefore, processes input and generates output at the same speed and for the same cost as a 12.9B model.