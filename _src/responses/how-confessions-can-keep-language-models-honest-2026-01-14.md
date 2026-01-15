---
title: "How confessions can keep language models honest"
targeturl: https://openai.com/index/how-confessions-can-keep-language-models-honest/
response_type: reshare
dt_published: "2026-01-14 22:44 -05:00"
dt_updated: "2026-01-14 22:44 -05:00"
tags: ["ai","openai","evaluations","research","llm"]
---

> This work explores one such approach: training models to explicitly admit when they engage in undesirable behavior—a technique we call confessions.  
> <br>
> A confession is a second output, separate from the model’s main answer to the user. The main answer is judged across many dimensions—correctness, style, helpfulness, compliance, safety, and more, and these multifaceted signals are used to train models to produce better answers. The confession, by contrast, is judged and trained on one thing only: honesty. Borrowing a page from the structure of a confessional, nothing the model says in its confession is held against it during training. If the model honestly admits to hacking a test, sandbagging, or violating instructions, that admission increases its reward rather than decreasing it. The goal is to encourage the model to faithfully report what it actually did.  
> <br>
In our tests, we found that the confessions method significantly improves the visibility of model misbehavior