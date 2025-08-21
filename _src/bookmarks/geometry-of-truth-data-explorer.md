---
title: "The Geometry of Truth: Dataexplorer"
targeturl:  https://saprmarks.github.io/geometry-of-truth/dataexplorer/
response_type: bookmark
dt_published: "2023-12-11 20:19"
dt_updated: "2023-12-11 20:19 -05:00"
tags: ["ai","interpretability","llm"]
---

> This page contains interactive charts for exploring how large language models represent truth. It accompanies the paper [The Geometry of Truth: Emergent Linear Structure in Large Language Model Representations of True/False Datasets](https://arxiv.org/abs/2310.06824) by Samuel Marks and Max Tegmark.
> 
> To produce these visualizations, we first extract [LLaMA-13B](https://ai.meta.com/blog/large-language-model-llama-meta-ai/) representations of factual statements. These representations live in a 5120-dimensional space, far too high-dimensional for us to picture, so we use [PCA](https://en.wikipedia.org/wiki/Principal_component_analysis) to select the two directions of greatest variation for the data. This allows us to produce 2-dimensional pictures of 5120-dimensional data. See this footnote for more details.[1](https://saprmarks.github.io/geometry-of-truth/dataexplorer/#fn:1)
