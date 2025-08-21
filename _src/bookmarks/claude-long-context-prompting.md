---
title: "Long context prompting for Claude 2.1"
targeturl: https://www.anthropic.com/index/claude-2-1-prompting
response_type: bookmark
dt_published: "2023-12-06 21:07"
dt_updated: "2023-12-06 21:07 -05:00"
tags: ["prompts","ai","llm"]
---

> - Claude 2.1 recalls information very well across its 200,000 token context window
>  - However, the model can be reluctant to answer questions based on an individual sentence in a document, especially if that sentence has been injected or is out of place
>  - A minor prompting edit removes this reluctance and results in excellent performance on these tasks

> What can users do if Claude is reluctant to respond to a long context retrieval question? We’ve found that a minor prompt update produces very different outcomes in cases where Claude is capable of giving an answer, but is hesitant to do so. When running the same evaluation internally, adding just one sentence to the prompt resulted in near complete fidelity throughout Claude 2.1’s 200K context window

> We achieved significantly better results on the same evaluation by adding the sentence “Here is the most relevant sentence in the context:” to the start of Claude’s response. This was enough to raise Claude 2.1’s score from 27% to 98% on the original evaluation.