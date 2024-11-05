---
title: "Microsoft Research: Introducing DRIFT Search"
targeturl: https://www.microsoft.com/en-us/research/blog/introducing-drift-search-combining-global-and-local-search-methods-to-improve-quality-and-efficiency/
response_type: reshare
dt_published: 2024-11-04 21:59 -05:00
dt_updated: 2024-11-04 21:59 -05:00
tags: ["ai","search","RAG","llm","microsoft","research","msr","graphrag"]
---

## Introducing DRIFT Search: Combining global and local search methods to improve quality and efficiency 

> DRIFT search (Dynamic Reasoning and Inference with Flexible Traversal)...builds upon Microsoft’s GraphRAG technique, combining characteristics of both global and local search to generate detailed responses in a method that balances computational costs with quality outcomes.

## DRIFT Search: A step-by-step process 

> 1. **Primer**: When a user submits a query, DRIFT compares it to the top K most semantically relevant community reports. This generates an initial answer along with several follow-up questions, which act as a lighter version of global search. To do this, we expand the query using Hypothetical Document Embeddings (HyDE), to increase sensitivity (recall), embed the query, look up the query against all community reports, select the top K and then use the top K to try to answer the query. The aim is to leverage high-level abstractions to guide further exploration.
> 2. **Follow-Up**: With the primer in place, DRIFT executes each follow-up using a local search variant. This yields additional intermediate answers and follow-up questions, creating a loop of refinement that continues until the search engine meets its termination criteria, which is currently configured for two iterations (further research will investigate reward functions to guide terminations). This phase represents a globally informed query refinement. Using global data structures, DRIFT navigates toward specific, relevant information within the knowledge graph even when the initial query diverges from the indexing persona. This follow-up process enables DRIFT to adjust its approach based on emerging information. 
> 3. **Output Hierarchy**: The final output is a hierarchy of questions and answers ranked on their relevance to the original query. This hierarchical structure can be customized to fit specific user needs. During benchmark testing, a naive map-reduce approach aggregated all intermediate answers, with each answer weighted equally. 

