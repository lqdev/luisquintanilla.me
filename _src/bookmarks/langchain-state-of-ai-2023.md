---
title: "LangChain State of AI 2023"
targeturl: https://blog.langchain.dev/langchain-state-of-ai-2023/
response_type: bookmark
dt_published: "2023-12-22 08:32"
dt_updated: "2023-12-22 08:32 -05:00"
tags: ["ai","llm","opensource","report"]
---

## What are people building?

> Retrieval has emerged as the dominant way to combine your data with LLMs. 

> ...42% of complex queries involve retrieval

> ...about 17% of complex queries are part of an agent.

## Most used LLM Providers

> OpenAI has emerged as the leading LLM provider of 2023, and Azure (with more enterprise guarantees) has seized that momentum well.

> On the open source model side, we see Hugging Face (4th), Fireworks AI (6th), and Ollama (7th) emerge as the main ways users interact with those models.

## OSS Model Providers

> A lot of attention recently has been given to open source models, with more and more providers racing to host them at cheaper and cheaper costs. So how exactly are developers accessing these open source models?
>   
> We see that the people are mainly running them locally, with options to do so like Hugging Face, LlamaCpp, Ollama, and GPT4All ranking high. 

## Most used vector stores

> Vectorstores are emerging as the primary way to retrieve relevant context.

> ...local vectorstores are the most used, with Chroma, FAISS, Qdrant and DocArray all ranking in the top 5. 

> Of the hosted offerings, Pinecone leads the pack as the only hosted vectorstore in the top 5. Weaviate follows next, showing that vector-native databases are currently more used than databases that add in vector functionality.

> Of databases that have added in vector functionality, we see Postgres (PGVector), Supabase, Neo4j, Redis, Azure Search, and Astra DB leading the pack.

## Most used embeddings

> OpenAI reigns supreme 

> Open source providers are more used, with Hugging Face coming in 2nd most use

> On the hosted side, we see that Vertex AI actually beats out AzureOpenAI

## Top Advanced Retrieval Strategies

> the most common retrieval strategy we see is not a built-in one but rather a custom one.

> After that, we see more familiar names popping up:
>   
>   - **Self Query** - which extracts metadata filters from user's questions
>   - **Hybrid Search** - mainly through provider specific integrations like Supabase and Pinecone
>   - **Contextual Compression** - which is postprocessing of base retrieval results
>   - **Multi Query** - transforming a single query into multiple, and then retrieving results for all
>   - **TimeWeighted VectorStore** - give more preference to recent documents

## How are people testing?

> 83% of test runs have some form of feedback associated with them. Of the runs with feedback, they average 2.3 different types of feedback, suggesting that developers are having difficulty finding a single metric to rely entirely on, and instead use multiple different metrics to evaluate.

> ...the majority of them use an LLM to evaluate the outputs. While some have expressed concern and hesitation around this, we are bullish on this as an approach and see that in practice it has emerged as the dominant way to test.

> ...nearly 40% of evaluators are custom evaluators. This is in line with the fact that we've observed that evaluation is often really specific to the application being worked on, and there's no one-size-fits-all evaluator to rely on.

## What are people testing?

> ...most people are still primarily concerned with the correctness of their application (as opposed to toxicity, prompt leakage, or other guardrails

> ...low usage of Exact Matching as an evaluation technique [suggests] that judging correctness is often quite complex (you can't just compare the output exactly as is)