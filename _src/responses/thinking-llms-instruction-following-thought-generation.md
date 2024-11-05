---
title: "Thinking LLMs: General Instruction Following with Thought Generation"
targeturl: https://arxiv.org/abs/2410.10630
response_type: reshare
dt_published: 2024-11-04 21:24 -05:00
dt_updated: 2024-11-04 21:24 -05:00
tags: ["ai","llm","research","meta","chainofthought","training","finetuning"]
---

> LLMs are typically trained to answer user questions or follow instructions similarly to how human experts respond. However, in the standard alignment framework they lack the basic ability of explicit thinking before answering. Thinking is important for complex questions that require reasoning and planning -- but can be applied to any task. We propose a training method for equipping existing LLMs with such thinking abilities for general instruction following without use of additional human data. We achieve this by an iterative search and optimization procedure that explores the space of possible thought generations, allowing the model to learn how to think without direct supervision. For each instruction, the thought candidates are scored using a judge model to evaluate their responses only, and then optimized via preference optimization. We show that this procedure leads to superior performance on AlpacaEval and Arena-Hard, and shows gains from thinking on non-reasoning categories such as marketing, health and general knowledge, in addition to more traditional reasoning & problem-solving tasks. 
