---
title: "Identifying Interactions at Scale for LLMs"
targeturl: https://bair.berkeley.edu/blog/2026/03/13/spex/
response_type: bookmark
dt_published: "2026-03-16 20:25 -05:00"
dt_updated: "2026-03-16 20:25 -05:00"
tags: ["research","ai","llm","explainability","interpretability"]
---

> ...Model behavior is rarely the result of isolated components; rather, it emerges from complex dependencies and patterns. To achieve state-of-the-art performance, models synthesize complex feature relationships, find shared patterns from diverse training examples, and process information through highly interconnected internal components.

> Therefore, grounded or reality-checked interpretability methods must also be able to capture these influential interactions. As the number of features, training data points, and model components grow, the number of potential interactions grows exponentially, making exhaustive analysis computationally infeasible. In this blog post, we describe the fundamental ideas behind [SPEX](https://openreview.net/forum?id=pRlKbAwczl) and [ProxySPEX](https://openreview.net/forum?id=KI8qan2EA7), algorithms capable of identifying these critical interactions at scale.

> Central to our approach is the concept of ablation, measuring influence by observing what changes when a component is removed.  
> <br> 
> - **Feature Attribution**: We mask or remove specific segments of the input prompt and measure the resulting shift in the predictions.
> - **Data Attribution**: We train models on different subsets of the training set, assessing how the model’s output on a test point shifts in the absence of specific training data.
> - **Model Component Attribution (Mechanistic Interpretability)**: We intervene on the model’s forward pass by removing the influence of specific internal components, determining which internal structures are responsible for the model’s prediction.  
> <br>
> In each case, the goal is the same: to isolate the drivers of a decision by systematically perturbing the system, in hopes of discovering influential interactions. Since each ablation incurs a significant cost, whether through expensive inference calls or retrainings, we aim to compute attributions with the fewest possible ablations.