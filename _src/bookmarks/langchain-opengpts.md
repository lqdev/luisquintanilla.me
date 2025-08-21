---
title: "LangChain - OpenGPTs"
targeturl: https://blog.langchain.dev/opengpts/
response_type: bookmark
dt_published: "2024-01-31 21:30 -05:00"
dt_updated: "2024-01-31 21:30 -05:00"
tags: ["langchain","gpt","ai","opengpt","langgraph","messagegraph"]
---

> A little over two months ago, on the heels of OpenAI dev day, we launched OpenGPTs: a take on what an open-source GPT store may look like. It was powered by an early version of LangGraph - an extension of LangChain aimed at building agents as graphs. At the time, we did not highlight this new package much, as we had not publicly launched it and were still figuring out the interface. We finally got around to launching LangGraph two weeks ago, and over the past weekend we updated OpenGPTs to fully use LangGraph (as well as added some new features). We figure now is as good of time as any to do a technical deep-dive on OpenGPTs and what powers it.  
> <br>
> In this blog, we will talk about:  
> <br>
>   - MessageGraph: A particular type of graph that OpenGPTs runs on  
>   - Cognitive architectures: What the 3 different types of cognitive architectures OpenGPTs supports are, and how they differ  
>   - Persistence: How persistence is baked in OpenGPTs via LangGraph checkpoints.  
>   - Configuration: How we use LangChain primitives to configure all these different bots.  
>   - New models: what new models we support  
>   - New tools: what new tools we support  
>   - astream_events: How we are using this new method to stream tokens and intermediate steps  
