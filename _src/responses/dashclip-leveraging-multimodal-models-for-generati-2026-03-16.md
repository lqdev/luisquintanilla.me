---
title: "DashCLIP: Leveraging multimodal models for generating semantic embeddings"
targeturl: https://careersatdoordash.com/blog/doordash-dashclip-multimodal-models-for-generating-semantic-embeddings/
response_type: reshare
dt_published: "2026-03-16 20:17 -05:00"
dt_updated: "2026-03-16 20:17 -05:00"
tags: ["doordash","ai","embeddings","search","machinelearning","platform"]
---

> To accommodate DoorDash’s continuing growth, the ads quality team set out to build foundational embeddings that can be reused across multiple use cases, such as retrieval, ranking, and relevance. Traditionally, the team has relied on categorical and numerical features such as store attributes, context features, and other handcrafted aggregates as inputs to our machine learning models. While these are important engagement signals, they fail to capture the rich semantic information contained in our product catalogs and don’t reflect a deeper understanding of users’ personal interests.  To bring these enhancements into our models, we developed DashCLIP, short for Dash Contrastive Language-Image Pretraining, a unified multimodal embedding framework designed to power personalized ad experiences for DoorDash users.

> DashCLIP’s architecture addresses the following functional requirements:  
> <br>
> - **Multimodality encodings**: Products on our platform contain both text and visual information. We leverage contrastive learning on the product catalog to approximate a human-like understanding of products, capturing the complementary information from each modality.
> - **Domain adaptation**: We perform continual pretraining on off-the-shelf models to adapt the embeddings to DoorDash’s data distribution. 
> - **Query embedding alignment**: To enable search recommendations, we introduce a second stage of alignment in our architecture for a dedicated query encoder that is trained to generate query embeddings in the same space as the product embeddings.
> - **Relevance dataset curation**: We curate a high-quality relevance dataset that combines internal human annotations with knowledge from large language models (LLMs), providing robust supervision for embedding alignment. This eliminates the position and selection bias introduced when historical engagement data is used for training.