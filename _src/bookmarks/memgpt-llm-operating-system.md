---
title: "MemGPT - Towards LLMs as Operating Systems"
targeturl: https://memgpt.ai/
response_type: bookmark
dt_published: "2023-12-11 19:58 -05:00"
dt_updated: "2023-12-11 19:58 -05:00"
tags: ["ai","os","llm"]
---

> Teach LLMs to manage their own memory for unbounded context!

> Large language models (LLMs) have revolutionized AI, but are constrained by limited context windows, hindering their utility in tasks like extended conversations and document analysis. To enable using context beyond limited context windows, we propose virtual context management, a technique drawing inspiration from hierarchical memory systems in traditional operating systems that provide the appearance of large memory resources through data movement between fast and slow memory. Using this technique, we introduce MemGPT (Memory-GPT), a system that intelligently manages different memory tiers in order to effectively provide extended context within the LLM's limited context window, and utilizes interrupts to manage control flow between itself and the user. We evaluate our OS-inspired design in two domains where the limited context windows of modern LLMs severely handicaps their performance: document analysis, where MemGPT is able to analyze large documents that far exceed the underlying LLM's context window, and multi-session chat, where MemGPT can create conversational agents that remember, reflect, and evolve dynamically through long-term interactions with their users. We release MemGPT code and data for our experiments at https://memgpt.ai. 

> In MemGPT, a fixed-context LLM processor is augmented with a tiered memory system and a set of functions that allow it to manage its own memory. Main context is the (fixed-length) LLM input. MemGPT parses the LLM text ouputs at each processing cycle, and either yields control or executes a function call, which can be used to move data between main and external context. When the LLM generates a function call, it can request immediate return of execution to chain together functions. In the case of a yield, the LLM will not be run again until the next external event trigger (e.g. a user message or scheduled interrupt). 