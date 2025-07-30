---
post_type: "note" 
title: "Ollama Adds Mistral 3.1 Support"
published_date: "2025-04-08 08:19"
tags: ["mistral","ollama","ai","llm"]
---

In the latest [Ollama 0.6.5 release](https://github.com/ollama/ollama/releases/tag/v0.6.5) support was added for [Mistral Small 3.1](https://ollama.com/library/mistral-small3.1). 

In an earlier post, I [highlighted the announcement and summarized the key features](/responses/mistral-small-3-1).

I've been using it with [GitHub Models](https://github.com/marketplace/models/azureml-mistral/mistral-small-2503) and based on vibes I found it was more reliable at structured output compared to Gemma 3, especially when considering the model and context window size.

Now that it's on Ollama thought, I can use it offline as well. However, I'm not sure how well that'll work on my ARM64 device, which only has 16GB of RAM.