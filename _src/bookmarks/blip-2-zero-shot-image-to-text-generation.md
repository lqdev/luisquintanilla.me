---
title: "Zero-shot image-to-text generation with BLIP-2"
targeturl: https://huggingface.co/blog/blip-2
response_type: bookmark
dt_published: "2023-02-15 14:04 -05:00"
dt_updated: "2023-02-15 14:04 -05:00"
---

> This guide introduces BLIP-2 from Salesforce Research that enables a suite of state-of-the-art visual-language models that are now available in 🤗 Transformers. We'll show you how to use it for image captioning, prompted image captioning, visual question-answering, and chat-based prompting.

> BLIP-2 bridges the modality gap between vision and language models by adding a lightweight Querying Transformer (Q-Former) between an off-the-shelf frozen pre-trained image encoder and a frozen large language model. Q-Former is the only trainable part of BLIP-2; both the image encoder and language model remain frozen. 
