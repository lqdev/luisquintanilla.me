---
title: "Quanto: a PyTorch quantization toolkit "
targeturl: https://huggingface.co/blog/quanto-introduction
response_type: reshare
dt_published: "2024-03-19 22:00"
dt_updated: "2024-03-19 22:00 -05:00"
tags: ["huggingface","quantization","pytorch","tools","ai"]
---

> Quantization is a technique to reduce the computational and memory costs of evaluating Deep Learning Models by representing their weights and activations with low-precision data types like 8-bit integer (int8) instead of the usual 32-bit floating point (float32).

> Today, we are excited to introduce quanto, a versatile pytorch quantization toolkit, that provides several unique features:  
><br>
>    - available in eager mode (works with non-traceable models)
>    - quantized models can be placed on any device (including CUDA and MPS),
>    - automatically inserts quantization and dequantization stubs,
>    - automatically inserts quantized functional operations,
>    - automatically inserts quantized modules (see below the list of supported modules),
>    - provides a seamless workflow for a float model, going from a dynamic to a static quantized model,
>    - supports quantized model serialization as a state_dict,
>    - supports not only int8 weights, but also int2 and int4,
>    - supports not only int8 activations, but also float8.

