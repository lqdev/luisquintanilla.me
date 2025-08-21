---
title: "OpenAI - Introducing improvements to the fine-tuning API and expanding our custom models program"
targeturl: https://openai.com/blog/introducing-improvements-to-the-fine-tuning-api-and-expanding-our-custom-models-program
response_type: reshare
dt_published: "2024-04-04 22:58 -05:00"
dt_updated: "2024-04-04 22:58 -05:00"
tags: ["openai","finetuning","llm"]
---

## New fine-tuning API features

> Today, we’re introducing new features to give developers even more control over their fine-tuning jobs, including:  
> <br>
> - Epoch-based Checkpoint Creation: Automatically produce one full fine-tuned model checkpoint during each training epoch, which reduces the need for subsequent retraining, especially in the cases of overfitting
> - Comparative Playground: A new side-by-side Playground UI for comparing model quality and performance, allowing human evaluation of the outputs of multiple models or fine-tune snapshots against a single prompt
> - Third-party Integration: Support for integrations with third-party platforms (starting with Weights and Biases this week) to let developers share detailed fine-tuning data to the rest of their stack
> - Comprehensive Validation Metrics: The ability to compute metrics like loss and accuracy over the entire validation dataset instead of a sampled batch, providing better insight on model quality
> - Hyperparameter Configuration: The ability to configure available hyperparameters from the Dashboard (rather than only through the API or SDK) 
> - Fine-Tuning Dashboard Improvements: Including the ability to configure hyperparameters, view more detailed training metrics, and rerun jobs from previous configurations

## Expanding our Custom Models Program

- Assisted Fine-Tuning

> Today, we are formally announcing our assisted fine-tuning offering as part of the Custom Model program. Assisted fine-tuning is a collaborative effort with our technical teams to leverage techniques beyond the fine-tuning API, such as additional hyperparameters and various parameter efficient fine-tuning (PEFT) methods at a larger scale. It’s particularly helpful for organizations that need support setting up efficient training data pipelines, evaluation systems, and bespoke parameters and methods to maximize model performance for their use case or task.

- Custom-Trained Model

> In some cases, organizations need to train a purpose-built model from scratch that understands their business, industry, or domain. Fully custom-trained models imbue new knowledge from a specific domain by modifying key steps of the model training process using novel mid-training and post-training techniques. Organizations that see success with a fully custom-trained model often have large quantities of proprietary data—millions of examples or billions of tokens—that they want to use to teach the model new knowledge or complex, unique behaviors for highly specific use cases. 