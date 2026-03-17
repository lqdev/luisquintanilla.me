---
title: "The Anatomy of an Agent Harness"
targeturl: https://blog.langchain.com/the-anatomy-of-an-agent-harness/
response_type: reshare
dt_published: "2026-03-16 20:39 -05:00"
dt_updated: "2026-03-16 20:39 -05:00"
tags: ["ai","agents"]
---

> TLDR: Agent = Model + Harness. Harness engineering is how we build systems around models to turn them into work engines. The model contains the intelligence and the harness makes that intelligence useful. We define what a harness is and derive the core components today's and tomorrow's agents need.

> A harness is every piece of code, configuration, and execution logic that isn't the model itself. A raw model is not an agent. But it becomes one when a harness gives it things like state, tool execution, feedback loops, and enforceable constraints.

> There are things we want an agent to do that a model cannot do out of the box. This is where a harness comes in.Models (mostly) take in data like text, images, audio, video and they output text. That's it. Out of the box they cannot:  
> <br>
> - Maintain durable state across interactions
> - Execute code
> - Access realtime knowledge
> - Setup environments and install packages to complete work  
> <br>
> **These are all harness level features**.