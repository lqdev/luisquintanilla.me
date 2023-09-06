---
title: "Can LLMs learn from a single example?"
targeturl: https://www.fast.ai/posts/2023-09-04-learning-jumps/
response_type: bookmark
dt_published: "2023-09-05 23:59"
dt_updated: "2023-09-05 23:59 -05:00"
tags: ["ai","llm","finetuning","fastai"]
---

> Summary: recently while fine-tuning a large language model (LLM) on multiple-choice science exam questions, we observed some highly unusual training loss curves. In particular, it appeared the model was able to rapidly memorize examples from the dataset after seeing them just once. This astonishing feat contradicts most prior wisdom about neural network sample efficiency. Intrigued by this result, we conducted a series of experiments to validate and better understand this phenomenon. It’s early days, but the experiments support the hypothesis that the models are able to rapidly remember inputs. This might mean we have to re-think how we train and use LLMs.
