---
title: "Introducing CodeMender: an AI agent for code security"
targeturl: https://deepmind.google/discover/blog/introducing-codemender-an-ai-agent-for-code-security/
response_type: reshare
dt_published: "2025-10-07 22:19 -05:00"
dt_updated: "2025-10-07 22:19 -05:00"
tags: ["agents","google","ai","deepmind","llm","gemini"]
---

> Today, we’re sharing early results from our research on CodeMender, a new AI-powered agent that improves code security automatically.

> CodeMender helps solve this problem by taking a comprehensive approach to code security that’s both reactive, instantly patching new vulnerabilities, and proactive, rewriting and securing existing code and eliminating entire classes of vulnerabilities in the process.

> As part of our research, we also developed new techniques and tools that let CodeMender reason about code and validate changes more effectively. This includes:  
> <br>
> - Advanced program analysis: We developed tools based on advanced program analysis that include static analysis, dynamic analysis, differential testing, fuzzing and SMT solvers. Using these tools to systematically scrutinize code patterns, control flow and data flow, CodeMender can better identify the root causes of security flaws and architectural weaknesses.
> - Multi-agent systems: We developed special-purpose agents that enable CodeMender to tackle specific aspects of an underlying problem. For example, CodeMender uses a large language model-based critique tool that highlights the differences between the original and modified code in order to verify that the proposed changes do not introduce regressions, and self-correct as needed.