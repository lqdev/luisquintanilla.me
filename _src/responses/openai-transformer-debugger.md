---
title: "OpenAI Transformer Debugger"
targeturl: https://github.com/openai/transformer-debugger
response_type: reshare
dt_published: "2024-03-17 21:05 -05:00"
dt_updated: "2024-03-17 21:05 -05:00"
tags: ["openai","ai","transformer","debugger","tools"]
---

> Transformer Debugger (TDB) is a tool developed by OpenAI's Superalignment team with the goal of supporting investigations into specific behaviors of small language models. The tool combines automated interpretability techniques with sparse autoencoders.  
> <br>
> TDB enables rapid exploration before needing to write code, with the ability to intervene in the forward pass and see how it affects a particular behavior. It can be used to answer questions like, "Why does the model output token A instead of token B for this prompt?" or "Why does attention head H attend to token T for this prompt?" It does so by identifying specific components (neurons, attention heads, autoencoder latents) that contribute to the behavior, showing automatically generated explanations of what causes those components to activate most strongly, and tracing connections between components to help discover circuits.
