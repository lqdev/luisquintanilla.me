---
title: "Introducing Metrax: performant, efficient, and robust model evaluation metrics in JAX"
targeturl: https://developers.googleblog.com/en/introducing-metrax-performant-efficient-and-robust-model-evaluation-metrics-in-jax/
response_type: reshare
dt_published: "2025-12-25 23:40 -05:00"
dt_updated: "2025-12-25 23:40 -05:00"
tags: ["ai","evaluations","jax","google","ml","machinelearning"]
---

> At Google, as teams were migrating from TensorFlow to JAX, teams were manually reimplementing metrics that were previously provided by TensorFlow, because JAX did not have a built-in metrics library. So each team using JAX was implementing its own version of accuracy, F1, RMS error, etc. While creating metrics may seem, to some, like a fairly simple and straightforward topic, when considering large scale training and evaluation across datacenter-sized distributed compute environments, it becomes somewhat less trivial.  
> <br>
> And thus the idea for Metrax was born: to bring a high-performance library for efficient and robust model evaluation metrics in JAX. [Metrax](https://metrax.readthedocs.io/) currently provides predefined metrics used to evaluate various types of machine learning models (classification, regression, recommendation, vision, audio, and language), and provides compatibility and consistency in distributed and scaled training environments. This allows you to focus on the model evaluation results, rather than (re)implementing various metrics definitions. Metrax adds to the ever-evolving ecosystem of JAX-based tooling, integrating well with the [JAX AI Stack](https://jaxstack.ai/), a suite of tools that are designed to work together to power your AI tooling needs. Today, Metrax is already used by some of the largest software stacks at Google, including teams in Google Search, YouTube, and Google’s own post-training library, Tunix.