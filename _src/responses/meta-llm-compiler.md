---
title: "Meta Large Language Model Compiler: Foundation Models of Compiler Optimization"
targeturl: https://huggingface.co/collections/facebook/llm-compiler-667c5b05557fe99a9edd25cb
response_type: reshare
dt_published: "2024-06-27 22:39"
dt_updated: "2024-06-27 22:39 -05:00"
tags: ["ai","compiler","meta","llm"]
---

> Large Language Models (LLMs) have demonstrated remarkable capabilities across a variety of software engineering and coding tasks. However, their application in the domain of code and compiler optimization remains underexplored. Training LLMs is resource-intensive, requiring substantial GPU hours and extensive data collection, which can be prohibitive. To address this gap, we introduce Meta Large Language Model Compiler (LLM Compiler), a suite of robust, openly available, pre-trained models specifically designed for code optimization tasks. Built on the foundation of Code Llama, LLM Compiler enhances the understanding of compiler intermediate representations (IRs), assembly language, and optimization techniques. The model has been trained on a vast corpus of 546 billion tokens of LLVM-IR and assembly code and has undergone instruction fine-tuning to interpret compiler behavior. LLM Compiler is released under a bespoke commercial license to allow wide reuse and is available in two sizes: 7 billion and 13 billion parameters. We also present fine-tuned versions of the model, demonstrating its enhanced capabilities in optimizing code size and disassembling from x86_64 and ARM assembly back into LLVM-IR. These achieve 77% of the optimising potential of an autotuning search, and 45% disassembly round trip (14% exact match). This release aims to provide a scalable, cost-effective foundation for further research and development in compiler optimization by both academic researchers and industry practitioners.

- [HuggingFace Collection](https://huggingface.co/collections/facebook/llm-compiler-667c5b05557fe99a9edd25cb)
- [Paper](https://scontent-lga3-1.xx.fbcdn.net/v/t39.2365-6/448997590_1496256481254967_2304975057370160015_n.pdf?_nc_cat=106&ccb=1-7&_nc_sid=3c67a6&_nc_ohc=4Yn8V9DFdbsQ7kNvgHtvfLI&_nc_ht=scontent-lga3-1.xx&oh=00_AYDYp657HzWrcs2CZ6ZBjStwB03bo760w9voXwJorfXA_w&oe=6683F28D)
