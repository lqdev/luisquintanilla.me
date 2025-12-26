---
title: "Token-count-based Batching: Faster, Cheaper Embedding Inference for Queries"
targeturl: https://mongodb.com/company/blog/engineering/token-count-based-batching-faster-cheaper-embedding-inference-for-queries
response_type: reshare
dt_published: "2025-12-25 23:01 -05:00"
dt_updated: "2025-12-25 23:01 -05:00"
tags: ["ai","embeddings","rag","search","mongodb"]
---

> In this blog post, we explore how batching can be used to serve queries more efficiently. We first discuss padding removal in modern inference engines, a key technique that enables effective batching. We then present practical strategies for forming batches and selecting an appropriate batch size. Finally, we walk through the implementation details and share the resulting performance improvements: a 50% reduction in GPU inference latency—despite using 3X fewer GPUs.