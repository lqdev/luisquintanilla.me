---
title: "TeLoGraF: Temporal Logic Planning via Graph-encoded Flow Matching"
targeturl: https://arxiv.org/abs/2505.00562
response_type: bookmark
dt_published: "2025-05-05 20:03"
dt_updated: "2025-05-05 20:03 -05:00"
tags: ["ai","graphs","neuralnetworks","research"]
---

> Learning to solve complex tasks with signal temporal logic (STL) specifications is crucial to many real-world applications. However, most previous works only consider fixed or parametrized STL specifications due to the lack of a diverse STL dataset and encoders to effectively extract temporal logic information for downstream tasks. **In this paper, we propose TeLoGraF, Temporal Logic Graph-encoded Flow, which utilizes Graph Neural Networks (GNN) encoder and flow-matching to learn solutions for general STL specifications.** We identify four commonly used STL templates and collect a total of 200K specifications with paired demonstrations. We conduct extensive experiments in five simulation environments ranging from simple dynamical models in the 2D space to high-dimensional 7DoF Franka Panda robot arm and Ant quadruped navigation. Results show that our method outperforms other baselines in the STL satisfaction rate. Compared to classical STL planning algorithms, our approach is 10-100X faster in inference and can work on any system dynamics. Besides, we show our graph-encoding method's capability to solve complex STLs and robustness to out-distribution STL specifications. Code is available at [this https URL](https://github.com/mengyuest/TeLoGraF)