---
title: "Introducing Locate 3D"
targeturl: https://locate3d.atmeta.com/
response_type: bookmark
dt_published: "2025-05-09 20:33 -05:00"
dt_updated: "2025-05-09 20:33 -05:00"
tags: ["meta","ai","3d","research"]
---

> Locate 3D transforms how AI understands physical space, delivering **state-of-the-art object localization in complex 3D environments**.  
> <br>
> Operating directly on standard sensor data, Locate 3D brings spatial intelligence to robotics and augmented reality applications in real-world settings.

[Paper](https://ai.meta.com/research/publications/locate-3d-real-world-object-localization-via-self-supervised-learning-in-3d/)

> We present Locate 3D, a model for localizing objects in 3D scenes from referring expressions like “the small coffee table between the sofa and the lamp.” Locate 3D sets a new state-of-the-art on standard referential grounding benchmarks and showcases robust generalization capabilities. Notably, Locate 3D operates directly on sensor observation streams (posed RGB-D frames), enabling real-world deployment on robots and AR devices. Key to our approach is 3D-JEPA, a novel self-supervised learning (SSL) algorithm applicable to sensor point clouds. It takes as input a 3D pointcloud featurized using 2D foundation models (CLIP, DINO). Subsequently, masked prediction in latent space is employed as a pretext task to aid the self-supervised learning of contextualized pointcloud features. Once trained, the 3D-JEPA encoder is finetuned alongside a language-conditioned decoder to jointly predict 3D masks and bounding boxes. Additionally, we introduce Locate 3D Dataset, a new dataset for 3D referential grounding, spanning multiple capture setups with over 130K annotations. This enables a systematic study of generalization capabilities as well as a stronger model.