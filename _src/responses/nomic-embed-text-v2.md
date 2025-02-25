---
title: "Nomic Embed Text V2: An Open Source, Multilingual, Mixture-of-Experts Embedding Model"
targeturl: https://www.nomic.ai/blog/posts/nomic-embed-text-v2
response_type: reshare
dt_published: "2025-02-24 20:47"
dt_updated: "2025-02-24 20:47 -05:00"
tags: ["nomic","ai","embeddings"]
---


> Today we're excited to announce Nomic Embed Text V2, our next-generation embedding model that brings the Mixture of Experts (MoE) architecture to text embeddings on a new expanded multilingual training dataset.

Personally, I found the part on MoE to be the most interesting about this release. 

> Rather than a dense model which uses all parameters on an input, the MoE architecture dynamically routes to different "experts" - sparse subsets of parameters at each layer - activating, ideally, only the parameters especially needed to process the input. This approach allows for more efficient use of compute when generating embeddings.  
> <br>
> In our experiments, we found that alternating MoE layers with 8 experts and top-2 routing provides the optimal balance between performance and efficiency. This results in 475M total parameters in the model, but only 305M active during training and inference.  
> <br>
Research into embedding model architecture has significant practical implications for working with text embeddings in production:  
> <br>
> - Lower latency for high-volume applications of embeddings like retrieval  
> <br>
> - Reduced deployment costs through more efficient parameter usage  
> <br>
> - More accessibility to embeddings in settings with constrained compute  