---
title: "Semantic Search and On-Device ML in Emacs"
targeturl: https://lepisma.xyz/2025/01/17/emacs-on-device-ml/index.html
response_type: reshare
dt_published: "2025-01-20 21:09"
dt_updated: "2025-01-20 21:09 -05:00"
tags: ["onnx","emacs","ai","ml"]
---

This is a cool walk-through of how to add semantic search to Emacs using local ML models. 

> With ONNX.el you can run any ML model inside Emacs, including ones for images, audios, etc. In case you are running embedding models, combine this with sem.el to perform semantic searches. Depending on your input modality, you might have to figure out preprocessing. For text, tokenizers.el should cover all of what you need in modern NLP for preprocessing, though you will have to do something on your own for anything non-text or multimodal.

> By default sem runs a local all-MiniLM-L6-v2 model for text embeddings. This model is small and runs fast on CPU. Additionally, we load an O2 optimized variant which helps further. The vectors are stored in a lancedb database on file system which runs without a separate process. I was originally writing the vector db core myself to learn more of Rust, but then stopped doing that since lancedb gives all that I needed, out of the box.