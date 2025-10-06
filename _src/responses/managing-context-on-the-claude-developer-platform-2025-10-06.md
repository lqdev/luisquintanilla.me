---
title: "Managing context on the Claude Developer Platform"
targeturl: https://www.anthropic.com/news/context-management
response_type: reply
dt_published: "2025-10-06 08:52 -05:00"
dt_updated: "2025-10-06 08:52 -05:00"
tags: ["claude","ai","llm","anthropic","contextengineering","agents"]
---

> Today, we’re introducing new capabilities for managing your agents’ context on the Claude Developer Platform: context editing and the memory tool.

> Context editing automatically clears stale tool calls and results from within the context window when approaching token limits. As your agent executes tasks and accumulates tool results, context editing removes stale content while preserving the conversation flow, effectively extending how long agents can run without manual intervention. This also increases the effective model performance as Claude focuses only on relevant context.

> The memory tool enables Claude to store and consult information outside the context window through a file-based system. Claude can create, read, update, and delete files in a dedicated memory directory stored in your infrastructure that persists across conversations. This allows agents to build up knowledge bases over time, maintain project state across sessions, and reference previous learnings without having to keep everything in context.