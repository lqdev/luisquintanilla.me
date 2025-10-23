---
title: "SentinelStep: Building agents that can wait, monitor, and act"
targeturl: https://www.microsoft.com/en-us/research/blog/tell-me-when-building-agents-that-can-wait-monitor-and-act/
response_type: reply
dt_published: "2025-10-22 22:07 -05:00"
dt_updated: "2025-10-22 22:07 -05:00"
tags: ["ai","research","microsoft","agents","msr","microsoftresearch","magentic","magenticui"]
---

> ...we are introducing SentinelStep(opens in new tab), a mechanism that enables agents to complete long-running monitoring tasks. The approach is simple. SentinelStep wraps the agent in a workflow with dynamic polling and careful context management. This enables the agent to monitor conditions for hours or days without getting sidetracked. We’ve implemented SentinelStep in Magentic-UI, our research prototype agentic system, to enable users to build agents for long-running tasks, whether they involve web browsing, coding, or external tools.