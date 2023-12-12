---
title: "Best Practices for LLM Evaluation of RAG Applications"
targeturl: https://www.databricks.com/blog/LLM-auto-eval-best-practices-RAG
response_type: bookmark
dt_published: "2023-12-11 19:54"
dt_updated: "2023-12-11 19:54 -05:00"
tags: ["ai","rag","evaluation","llm"]
---

> This blog represents the first in a series of investigations we’re running at Databricks to provide learnings on LLM evaluation.

> Recently, the LLM community has been exploring the use of “LLMs as a judge” for automated evaluation with many using powerful LLMs such as GPT-4 to do the evaluation for their LLM outputs. 

> Using the Few Shots prompt with GPT-4 didn’t make an obvious difference in the consistency of results.

> Including few examples for GPT-3.5-turbo-16k significantly improves the consistency of the scores, and makes the result usable. 

> ...evaluation results can’t be transferred between use cases and we need to build use-case-specific benchmarks in order to properly evaluate how good a model can meet customer needs.
