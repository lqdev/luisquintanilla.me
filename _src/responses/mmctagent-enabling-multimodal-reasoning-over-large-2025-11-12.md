---
title: "MMCTAgent: Enabling multimodal reasoning over large video and image collections"
targeturl: https://www.microsoft.com/en-us/research/blog/mmctagent-enabling-multimodal-reasoning-over-large-video-and-image-collections/
response_type: reshare
dt_published: "2025-11-12 16:11 -05:00"
dt_updated: "2025-11-12 16:11 -05:00"
tags: ["ai","video","agents","research","multimodal","microsoft"]
---

> "...we developed the Multi-modal Critical Thinking Agent, or MMCTAgent, for structured reasoning over long-form video and image data, available on GitHub and featured on Azure AI Foundry Labs.  
> <br>
> Built on AutoGen, Microsoft’s open-source multi-agent system, MMCTAgent provides multimodal question-answering with a Planner–Critic architecture. This design enables planning, reflection, and tool-based reasoning, bridging perception and deliberation in multimodal tasks. It links language, vision, and temporal understanding, transforming static multimodal tasks into dynamic reasoning workflows.  
> <br>
> Unlike conventional models that produce one-shot answers, MMCTAgent has modality-specific agents, including ImageAgent and VideoAgent, which include tools like get_relevant_query_frames() or object_detection-tool(). These agents perform deliberate, iterative reasoning—selecting the right tools for each modality, evaluating intermediate results, and refining conclusions through a Critic loop. This enables MMCTAgent to analyze complex queries across long videos and large image libraries with explainability, extensibility, and scalability."