---
title: "Predictive Human Preference: From Model Ranking to Model Routing"
targeturl: https://huyenchip.com//2024/02/28/predictive-human-preference.html
response_type: reshare
dt_published: "2024-02-29 10:43 -05:00"
dt_updated: "2024-02-29 10:43 -05:00"
tags: ["ai","llm","evaluation","ml"]
---

> Human preference has emerged to be both the Northstar and a powerful tool for AI model development. Human preference guides post-training techniques including RLHF and DPO. Human preference is also used to rank AI models, as used by LMSYS’s Chatbot Arena.

> Chatbot Arena aims to determine which model is generally preferred. I wanted to see if it’s possible to do predictive human preference: determine which model is preferred for each query.

> This post first discusses the correctness of Chatbot Arena, which will then be used as a baseline to evaluate the correctness of preference predictions. It then discusses how to build a preference predictor and the initial results.
