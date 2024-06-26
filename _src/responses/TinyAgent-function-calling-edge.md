---
title: "TinyAgent: Function Calling at the Edge"
targeturl: https://bair.berkeley.edu/blog/2024/05/29/tiny-agent/
response_type: reshare
dt_published: "2024-06-11 19:43"
dt_updated: "2024-06-11 19:43 -05:00"
tags: ["agent","ai","slm","function","edge","local","languagemodel","smalllanguagemodel","research"]
---

![High level conceptual diagram of TinyAgent system](https://bair.berkeley.edu/static/blog/tiny-agent/Figure2.png)

> The ability of LLMs to execute commands through plain language (e.g. English) has enabled agentic systems that can complete a user query by orchestrating the right set of tools...recent multi-modal efforts such as the GPT-4o or Gemini-1.5 model, has expanded the realm of possibilities with AI agents. While this is quite exciting, the large model size and computational requirements of these models often requires their inference to be performed on the cloud. This can create several challenges for their widespread adoption. First and foremost, uploading data such as video, audio, or text documents to a third party vendor on the cloud, can result in privacy issues. Second, this requires cloud/Wi-Fi connectivity which is not always possible...latency could also be an issue as uploading large amounts of data to the cloud and waiting for the response could slow down response time, resulting in unacceptable time-to-solution. These challenges could be solved if we deploy the LLM models locally at the edge.

> ...current LLMs like GPT-4o or Gemini-1.5 are too large for local deployment. One contributing factor is that a lot of the model size ends up memorizing general information about the world into its parametric memory which may not be necessary for a specialized downstream application.

> ...this leads to an intriguing research question:  
> <br>
> Can a smaller language model with significantly less parametric memory emulate such emergent ability of these larger language models?

> Achieving this would significantly reduce the computational footprint of agentic systems and thus enable efficient and privacy-preserving edge deployment. Our study demonstrates that this is feasible for small language models through training with specialized, high-quality data that does not require recalling generic world knowledge.  
> <br>
> Such a system could particularly be useful for semantic systems where the AI agent’s role is to understand the user query in natural language and, instead of responding with a ChatGPT-type question answer response, orchestrate the right set of tools and APIs to accomplish the user’s command. For example, in a Siri-like application, a user may ask a language model to create a calendar invite with particular attendees. If a predefined script for creating calendar items already exists, the LLM simply needs to learn how to invoke this script with the correct input arguments (such as attendees’ email addresses, event title, and time). This process does not require recalling/memorization of world knowledge from sources like Wikipedia, but rather requires reasoning and learning to call the right functions and to correctly orchestrate them.  
> <br>
> Our goal is to develop Small Language Models (SLM) that are capable of complex reasoning that could be deployed securely and privately at the edge. Here we will discuss the research directions that we are pursuing to that end. First, we discuss how we can enable small open-source models to perform accurate function calling, which is a key component of agentic systems. It turns out that off-the-shelf small models have very low function calling capabilities. We discuss how we address this by systematically curating high-quality data for function calling, using a specialized Mac assistant agent as our driving application. We then show that fine-tuning the model on this high quality curated dataset, can enable SLMs to even exceed GPT-4-Turbo’s function calling performance. We then show that this could be further improved and made efficient through a new Tool RAG method. Finally, we show how the final models could be deployed efficiently at the edge with real time responses.
