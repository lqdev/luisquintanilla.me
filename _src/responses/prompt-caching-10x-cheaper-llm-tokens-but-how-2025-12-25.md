---
title: "Prompt caching: 10x cheaper LLM tokens, but how?"
targeturl: https://ngrok.com/blog/prompt-caching/
response_type: reshare
dt_published: "2025-12-25 23:56 -05:00"
dt_updated: "2025-12-25 23:56 -05:00"
tags: ["ai","cost","llm","caching"]
---

> As I write this post, cached input tokens are 10x cheaper in dollars per token than regular input tokens for both OpenAI and Anthropic's APIs.  
> <br>
> Anthropic even claim that prompt caching can reduce latency "by up to 85% for long prompts" and in my own testing I found that for a long enough prompt, this is true. I sent hundreds of requests to both Anthropic and OpenAI and noticed a substantial reduction in time-to-first-token latency for prompts where every input token was cached.

> By the end of this post you will...  
> <br>
> - Understand, at a deeper level, how LLMs work
> - Have built some new intuition for why LLMs work the way they do
> - Understand the exact 1s and 0s that get cached, and how they reduce the cost of your LLM requests