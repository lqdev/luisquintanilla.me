---
title: "Effective context engineering for AI agents"
targeturl: "https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents"
response_type: "reshare"
dt_published: "2025-10-01 18:22 -05:00"
dt_updated: "2025-10-01 18:22 -05:00"
tags: ["contextengineering","ai","agents","llm","anthropic"]
---

> In this post, we’ll explore the emerging art of context engineering and offer a refined mental model for building steerable, effective agents.

> Context engineering refers to the set of strategies for curating and maintaining the optimal set of tokens (information) during LLM inference, including all the other information that may land there outside of the prompts.

> Given that LLMs are constrained by a finite attention budget, good context engineering means finding the smallest possible set of high-signal tokens that maximize the likelihood of some desired outcome.

> Whether you're implementing compaction for long-horizon tasks, designing token-efficient tools, or enabling agents to explore their environment just-in-time, the guiding principle remains the same: find the smallest set of high-signal tokens that maximize the likelihood of your desired outcome.