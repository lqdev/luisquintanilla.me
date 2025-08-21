---
post_type: "note" 
title: "These models are too damn big!"
published_date: "2024-04-08 23:43 -05:00"
tags: ["ai","slm","huggingface","llm","opensource"]
---

While the size of these smaller language models is significantly less than the trillion parameter models like GPT, they still take up a lot of storage space. Playing around with [Mistral 7B Instruct v0.2](https://huggingface.co/mistralai/Mistral-7B-Instruct-v0.2), the safetensor files containing the weights take up roughly 15GB of space. I'm thinking of playing around with [blobfuse](https://learn.microsoft.com/azure/storage/blobs/blobfuse2-what-is) to mount a storage container to my local file system. That way, I'm only caching and accessing the models I'm working with at any given time. 