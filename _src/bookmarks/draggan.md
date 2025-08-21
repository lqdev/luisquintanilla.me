---
title: "Drag Your GAN: Interactive Point-based Manipulation on the Generative Image Manifold"
targeturl: https://arxiv.org/abs/2305.10973 
response_type: bookmark
dt_published: "2023-06-01 22:52 -05:00"
dt_updated: "2023-06-01 22:52 -05:00"
tags: ["ai","computer-vision"]
---

> ...we propose DragGAN, which consists of two main components: 1) a feature-based motion supervision that drives the handle point to move towards the target position, and 2) a new point tracking approach that leverages the discriminative generator features to keep localizing the position of the handle points. Through DragGAN, anyone can deform an image with precise control over where pixels go, thus manipulating the pose, shape, expression, and layout of diverse categories such as animals, cars, humans, landscapes, etc. As these manipulations are performed on the learned generative image manifold of a GAN, they tend to produce realistic outputs even for challenging scenarios such as hallucinating occluded content and deforming shapes that consistently follow the object's rigidity. Both qualitative and quantitative comparisons demonstrate the advantage of DragGAN over prior approaches in the tasks of image manipulation and point tracking. We also showcase the manipulation of real images through GAN inversion. 

[Code](https://github.com/XingangPan/DragGAN)