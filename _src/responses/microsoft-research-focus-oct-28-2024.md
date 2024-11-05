---
title: "Microsoft Research Focus: Week of October 28, 2024"
targeturl: https://www.microsoft.com/en-us/research/blog/research-focus-week-of-october-28-2024/
response_type: reshare
dt_published: 2024-11-04 21:39 -05:00
dt_updated: 2024-11-04 21:39 -05:00
tags: ["ai","microsoft","research","msr","microsoftresearch"]
---

## METAREFLECTION: Learning Instructions for Language Agents using Past Reflections

> Language agents are AI systems that can understand, reason and respond in natural language to complete various tasks. While the latest LLMs are capable enough to power reasonably good language agents, the closed-API model makes it hard to improve them when they perform sub-optimally. Recent studies have explored using techniques like self-reflection and prompt optimization to improve performance. Unfortunately, self-reflection can be used only during the agent’s current run, while contemporary prompt optimization techniques are designed and tested to work on simple single-step agents.  
>  
> In a recent paper: METAREFLECTION: Learning Instructions for Language Agents using Past Reflections, researchers from Microsoft introduce a novel offline reinforcement learning technique that enhances the performance of language agents by augmenting a semantic memory based on experiential learnings from past trials. They demonstrate the efficacy of METAREFLECTION across multiple domains, including complex logical reasoning, biomedical semantic similarity, open world question answering, and vulnerability threat detection, in Infrastructure-as-Code, spanning different agent designs. METAREFLECTION boosts language agents’ performance by 4% to 16.82% over the baseline agent implementations and performs on par with existing state-of-the-art prompt optimization techniques while requiring fewer LLM calls. 


## Domino: Eliminating Communication in LLM Training via Generic Tensor Slicing and Overlapping

> Generative AI applications rely on large, foundation models, particularly LLMs. LLMs often have tens to hundreds of billions of parameters, making them too large for a single graphics processing unit (GPU) to handle in terms of both memory and computation. Because of their size, training these models requires distributing the workload across hundreds or even thousands of GPUs. This can lead to significant communication overhead, a challenge that arises when data needs to be shared between different GPUs.  
> 
> In a recent paper: Domino: Eliminating Communication in LLM Training via Generic Tensor Slicing and Overlapping, researchers from Microsoft introduce a system designed to enhance the efficiency of LLM training by reducing the time lost to communication between GPUs.  
> 
> Domino breaks down data dependencies in a single batch of training into smaller, independent pieces. These smaller pieces are processed in parallel, and communication between GPUs happens simultaneously with computation, minimizing delays.  
> 
> Test results comparing Domino to Megatron-LM show that Domino speeds up the training process by up to 1.3x on Nvidia DGX-H100 GPUs.

## OmniParser for pure vision-based GUI agent

> Large vision-language models (VLMs) such as GPT-4V and GPT-4o show promise in driving intelligent agent systems that operate within user interfaces (UI). However, VLMs’ full potential remains underexplored in real-world applications, particularly when it comes to acting as general agents across diverse operating systems and applications with only vision input. One limiting factor is the absence of a robust technique for screen parsing which is capable of 1) reliably identifying interactable icons within the user interface, and 2) understanding the semantics of various elements in a screenshot and accurately associating the intended action with the corresponding region on the screen.  
>  
> In a recent article: OmniParser for pure vision-based GUI agent, researchers from Microsoft present a compact screen parsing module that can convert UI screenshots into structured elements. OmniParser can be used with a variety of models to create agents capable of taking actions on UIs. When used with GPT-4V, OmniParser significantly improves the agent capability to generate precisely grounded actions for interface regions.  
>  
> OmniParser with GPT-4V agent achieved the best performance on the recently released  WindowsAgentArena (opens in new tab) benchmark.  
