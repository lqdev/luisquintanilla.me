---
title: "Chroma Context-1: Training a Self-Editing Search Agent·|·Chroma"
targeturl: https://www.trychroma.com/research/context-1?utm_source=tldrai
response_type: reshare
dt_published: "2026-03-28 11:15 -05:00"
dt_updated: "2026-03-28 11:15 -05:00"
tags: ["ai","rag","search","chroma","agents"]
---

> Retrieval pipelines typically operate in a single pass, which poses a problem when the information required to answer a question is spread across multiple documents or requires intermediate reasoning to locate. In practice, many real-world queries require multi-hop retrieval, in which the output of one search informs the next. Recent work has shown that frontier LLMs perform this multi-hop search effectively through a process known as agentic search, simply defined as a loop of LLM calls with search tools. This mode of search often comes with significant cost and latency due to their use of frontier-scale LLMs.  
> <br>
> We introduce Chroma Context-1, a 20B parameter agentic search model derived from gpt-oss-20B that achieves retrieval performance comparable to frontier-scale LLMs at a fraction of the cost and up to 10x faster inference speed. Context-1 is designed to be used as a subagent in conjunction with a frontier reasoning model. Given a query, it produces a ranked list of documents that are relevant to satisfying the query. The model is trained to decompose queries into subqueries, iteratively search a corpus, and selectively edit its own context to free capacity for further exploration.