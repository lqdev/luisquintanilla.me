---
title: "SAMMO: A general-purpose framework for prompt optimization"
targeturl: https://www.microsoft.com/en-us/research/blog/sammo-a-general-purpose-framework-for-prompt-optimization/
response_type: reshare
dt_published: "2024-04-25 22:48 -05:00"
dt_updated: "2024-04-25 22:48 -05:00"
tags: ["ai","microsoft","research","prompt","optimization","sammo"]
---

> Large language models (LLMs) have revolutionized a wide range of tasks and applications that were previously reliant on manually crafted machine learning (ML) solutions, streamlining through automation. However, despite these advances, a notable challenge persists: the need for extensive prompt engineering to adapt these models to new tasks. New generations of language models like GPT-4 and Mixtral 8x7B advance the capability to process long input texts. This progress enables the use of longer inputs, providing richer context and detailed instructions to language models. A common technique that uses this enhanced capacity is the Retrieval Augmented Generation (RAG) approach. RAG dynamically incorporates information into the prompt based on the specific input example.

> To address these challenges, we developed the Structure-Aware Multi-objective Metaprompt Optimization (SAMMO) framework. SAMMO is a new open-source tool that streamlines the optimization of prompts, particularly those that combine different types of structural information like in the RAG example above. It can make structural changes, such as removing entire components or replacing them with different ones. These features enable AI practitioners and researchers to efficiently refine their prompts with little manual effort.  
> <br>
Central to SAMMO’s innovation is its approach to treating prompts not just as static text inputs but as dynamic, programmable entities—metaprompts. SAMMO represents these metaprompts as function graphs, where individual components and substructures can be modified to optimize performance, similar to the optimization process that occurs during traditional program compilation.  
> <br>
The following key features contribute to SAMMO’s effectiveness:  
> <br>
> - Structured optimization: Unlike current methods that focus on text-level changes, SAMMO focuses on optimizing the structure of metaprompts. This granular approach facilitates precise modifications and enables the straightforward integration of domain knowledge, for instance, through rewrite operations targeting specific stylistic objectives.  
> <br>
> - Multi-objective search: SAMMO’s flexibility enables it to simultaneously address multiple objectives, such as improving accuracy and computational efficiency. Our paper illustrates how SAMMO can be used to compress prompts without compromising their accuracy.  
> <br>
> - General purpose application: SAMMO has proven to deliver significant performance improvements across a variety of tasks, including instruction tuning, RAG, and prompt compression.