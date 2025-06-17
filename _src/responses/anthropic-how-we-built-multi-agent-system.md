---
title: "How we built our multi-agent research system - Anthropic"
targeturl: https://www.anthropic.com/engineering/built-multi-agent-research-system 
response_type: reshare
dt_published: "2025-06-16 17:39"
dt_updated: "2025-06-16 20:30 -05:00"
tags: ["agent","ai","anthropic","engineering"]
---

Great article. 

TLDR: These are largely engineering problems.

> The journey of this multi-agent system from prototype to production taught us critical lessons about system architecture, tool design, and prompt engineering. A multi-agent system consists of multiple agents (**LLMs autonomously using tools in a loop**) working together. Our Research feature involves an agent that plans a research process based on user queries, and then uses tools to create parallel agents that search for information simultaneously. Systems with multiple agents introduce new challenges in agent coordination, evaluation, and reliability.

##  Benefits of a multi-agent system

> The essence of search is compression: distilling insights from a vast corpus. **Subagents facilitate compression by operating in parallel** with their own context windows, exploring different aspects of the question simultaneously before condensing the most important tokens for the lead research agent. Each subagent also provides **separation of concerns**—distinct tools, prompts, and exploration trajectories—which reduces path dependency and enables thorough, independent investigations.

> Once intelligence reaches a threshold, multi-agent systems become a vital way to scale performance. For instance, although individual humans have become more intelligent in the last 100,000 years, human societies have become exponentially more capable in the information age because of our collective intelligence and ability to coordinate. Even generally-intelligent agents face limits when operating as individuals; **groups of agents can accomplish far more**.

> Our internal evaluations show that multi-agent research systems excel especially for breadth-first queries that involve pursuing multiple independent directions simultaneously.

> Multi-agent systems work mainly because they help spend enough tokens to solve the problem...Multi-agent architectures effectively scale token usage for tasks that exceed the limits of single agents.

> We’ve found that multi-agent systems excel at valuable tasks that involve heavy parallelization, information that exceeds single context windows, and interfacing with numerous complex tools.

## Architecture overview

> Our Research system uses a multi-agent architecture with an orchestrator-worker pattern, where a lead agent coordinates the process while delegating to specialized subagents that operate in parallel.

> Traditional approaches using Retrieval Augmented Generation (RAG) use static retrieval. That is, they fetch some set of chunks that are most similar to an input query and use these chunks to generate a response. In contrast, our architecture uses a **multi-step search that dynamically finds relevant information, adapts to new findings, and analyzes results to formulate high-quality answers**.

## Prompt engineering and evaluations for research agents

> **Think like your agents.** To iterate on prompts, you must understand their effects. To help us do this, we built simulations using our Console with the exact prompts and tools from our system, then watched agents work step-by-step.

> **Teach the orchestrator how to delegate.** In our system, the lead agent decomposes queries into subtasks and describes them to subagents. Each subagent needs an **objective**, an **output format**, **guidance on the tools and sources to use**, and **clear task boundaries**. Without detailed task descriptions, agents duplicate work, leave gaps, or fail to find necessary information.

> **Scale effort to query complexity.** Agents struggle to judge appropriate effort for different tasks, so we embedded scaling rules in the prompts...

> **Tool design and selection are critical.** Agent-tool interfaces are as critical as human-computer interfaces. Using the right tool is efficient—often, it’s strictly necessary...Bad tool descriptions can send agents down completely wrong paths, so each tool needs a distinct purpose and a clear description.

> **Let agents improve themselves.**...When given a prompt and a failure mode, they are able to diagnose why the agent is failing and suggest improvements. 

> **Start wide, then narrow down.** Search strategy should mirror expert human research: explore the landscape before drilling into specifics.

> **Guide the thinking process.**...Our testing showed that extended thinking improved instruction-following, reasoning, and efficiency. Subagents also plan, then use interleaved thinking after tool results to evaluate quality, identify gaps, and refine their next query. This makes subagents more effective in adapting to any task.

> **Parallel tool calling transforms speed and performance.**...For speed, we introduced two kinds of parallelization: (1) the lead agent spins up 3-5 subagents in parallel rather than serially; (2) the subagents use 3+ tools in parallel.

> Our prompting strategy focuses on instilling **good heuristics rather than rigid rules**. We studied how skilled humans approach research tasks and encoded these strategies in our prompts—strategies like **decomposing difficult questions into smaller tasks**, **carefully evaluating the quality of sources**, **adjusting search approaches based on new information**, and **recognizing when to focus on depth (investigating one topic in detail) vs. breadth (exploring many topics in parallel)**.

## Effective evaluation of agents

> ...evaluating multi-agent systems presents unique challenges...Because we don’t always know what the right steps are, we usually can't just check if agents followed the “correct” steps we prescribed in advance. Instead, we need flexible evaluation methods that judge whether agents achieved the right outcomes while also following a reasonable process.

> **Start evaluating immediately with small samples.**...it’s best to start with small-scale testing right away with a few examples, rather than delaying until you can build more thorough evals.

> **LLM-as-judge evaluation scales when done well.**...We used an LLM judge that evaluated each output against criteria in a rubric: **factual accuracy** (do claims match sources?), **citation accuracy** (do the cited sources match the claims?), **completeness** (are all requested aspects covered?), **source quality** (did it use primary sources over lower-quality secondary sources?), and **tool efficiency** (did it use the right tools a reasonable number of times?)...[we] found that a single LLM call with a single prompt outputting scores from 0.0-1.0 and a pass-fail grade was the most consistent and aligned with human judgements.

> **Human evaluation catches what automation misses.**...Adding source quality heuristics to our prompts helped resolve this issue. Even in a world of automated evaluations, manual testing remains essential.

> the best prompts for these agents are not just strict instructions, but **frameworks for collaboration** that define the **division of labor**, **problem-solving approaches**, and **effort budgets**. Getting this right relies on **careful prompting and tool design**, **solid heuristics**, **observability**, and **tight feedback loops**.

## Production reliability and engineering challenges

> **Agents are stateful and errors compound.**...Agents can run for long periods of time, maintaining state across many tool calls. This means we need to **durably execute code and handle errors along the way**. Without effective mitigations, minor system failures can be catastrophic for agents. When errors occur, we can't just restart from the beginning...We combine the adaptability of AI agents built on Claude with deterministic safeguards like retry logic and regular checkpoints.

> **Debugging benefits from new approaches**...Adding full production tracing let us diagnose why agents failed and fix issues systematically. Beyond standard observability, we monitor agent decision patterns and interaction structures—all without monitoring the contents of individual conversations, to maintain user privacy.

> **Deployment needs careful coordination**...we use rainbow deployments to avoid disrupting running agents, by gradually shifting traffic from old to new versions while keeping both running simultaneously.

> **Synchronous execution creates bottlenecks.** Currently, our lead agents execute subagents synchronously, waiting for each set of subagents to complete before proceeding. This simplifies coordination, but creates bottlenecks in the information flow between agents. Asynchronous execution would enable additional parallelism: agents working concurrently and creating new subagents when needed. But this asynchronicity adds challenges in result coordination, state consistency, and error propagation across the subagents. As models can handle longer and more complex research tasks, we expect the performance gains will justify the complexity.

## Conclusion

> For all the reasons described in this post, the gap between prototype and production is often wider than anticipated.

> Multi-agent research systems can operate reliably at scale with **careful engineering**, **comprehensive testing**, **detail-oriented prompt and tool design**, **robust operational practices**, and **tight collaboration between research, product, and engineering teams** who have a strong understanding of current agent capabilities.