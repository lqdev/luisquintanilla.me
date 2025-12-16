---
title: "From Single-Node to Multi-GPU Clusters: How Discord Made Distributed Compute Easy for ML Engineers"
targeturl: https://discord.com/blog/from-single-node-to-multi-gpu-clusters-how-discord-made-distributed-compute-easy-for-ml-engineers
response_type: reshare
dt_published: "2025-12-15 22:15 -05:00"
dt_updated: "2025-12-15 22:15 -05:00"
tags: ["ai","ray","kubernetes","discord","distributedsystems","distributedml"]
---

> At Discord, our machine learning systems have evolved from simple classifiers to sophisticated models serving hundreds of millions of users. As our models grew more complex and datasets larger, we increasingly ran into scaling challenges: training jobs that needed multiple GPUs, datasets that wouldn’t fit on single machines, and computational demands that outpaced our infrastructure.  
> <br>
> Access to distributed compute was necessary — but not sufficient. We needed distributed ML to be easy. Ray, an open-source distributed computing framework, became our foundation. At Discord, we built a platform around it: custom CLI tooling, orchestration with Dagster + KubeRay, and an observability layer called X-Ray. Our focus was on developer experience, turning distributed ML from something hard to use into a system they are excited to work with.  
> <br>
> This is how Discord went from no deep learning, to ad-hoc experiments, to a production orchestration platform, and how that work enabled models like Ads Ranking that delivered a +200% improvement on our business metrics.