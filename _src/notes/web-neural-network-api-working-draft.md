---
post_type: "note" 
title: "Web Neural Network API - Working Draft"
published_date: "10/24/2021 20:36"
tags: ["webnn","ai","neuralnetwork","w3c","standards","api","protocol"]
---

While browsing the interwebs I came across the [Web Neural Network API](https://www.w3.org/TR/webnn/) spec from the [W3C Web Machine Learning Working Group](https://www.w3.org/groups/wg/webmachinelearning). The abstract defines it as "a dedicated low-level API for neural network inference hardware acceleration.". Although there are already a few frameworks like ONNX & TensorFlow.js that allow you to inference in the browser, this spec looks interesting because it provides an abstraction that allows you to take advantage of hardware acceleration using the framework of your choice. As the [explainer document](https://github.com/webmachinelearning/webnn/blob/main/explainer.md) mentions, "this architecture allows JavaScript frameworks to tap into cutting-edge machine learning innovations in the operating system and the hardware platform underneath it without being tied to platform-specific capabilities, bridging the gap between software and hardware through a hardware-agnostic abstraction layer.". It's just a working draft for now but I'm looking forward to how this develops.