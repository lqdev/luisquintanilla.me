---
title: "Shap-E: Generating Conditional 3D Implicit Functions"
targeturl: https://arxiv.org/abs/2305.02463
response_type: bookmark
dt_published: "2023-06-01 22:41"
dt_updated: "2023-06-01 22:41 -05:00"
tags: ["ai","openai","3D"]
---

> We present Shap-E, a conditional generative model for 3D assets. Unlike recent work on 3D generative models which produce a single output representation, Shap-E directly generates the parameters of implicit functions that can be rendered as both textured meshes and neural radiance fields. We train Shap-E in two stages: first, we train an encoder that deterministically maps 3D assets into the parameters of an implicit function; second, we train a conditional diffusion model on outputs of the encoder. When trained on a large dataset of paired 3D and text data, our resulting models are capable of generating complex and diverse 3D assets in a matter of seconds. When compared to Point-E, an explicit generative model over point clouds, Shap-E converges faster and reaches comparable or better sample quality despite modeling a higher-dimensional, multi-representation output space.

[Code](https://github.com/openai/shap-e)

