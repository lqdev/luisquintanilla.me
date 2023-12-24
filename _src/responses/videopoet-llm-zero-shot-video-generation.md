---
title: "VideoPoet: A large language model for zero-shot video generation"
targeturl: https://sites.research.google/videopoet/
response_type: bookmark
dt_published: "2023-12-23 19:06"
dt_updated: "2023-12-23 19:06 -05:00"
tags: ["ai","genai","videopoet","generativeai","google"]
---

> VideoPoet is a simple modeling method that can convert any autoregressive language model or large language model (LLM) into a high-quality video generator. It contains a few simple components:  
>   
>    - A pre-trained MAGVIT V2 video tokenizer and a SoundStream audio tokenizer transform images, video, and audio clips with variable lengths into a sequence of discrete codes in a unified vocabulary. These codes are compatible with text-based language models, facilitating an integration with other modalities, such as text.
>    - An autoregressive language model learns across video, image, audio, and text modalities to autoregressively predict the next video or audio token in the sequence.
>    - A mixture of multimodal generative learning objectives are introduced into the LLM training framework, including text-to-video, text-to-image, image-to-video, video frame continuation, video inpainting and outpainting, video stylization, and video-to-audio. Furthermore, such tasks can be composed together for additional zero-shot capabilities (e.g., text-to-audio).  
>  
> This simple recipe shows that language models can synthesize and edit videos with a high degree of temporal consistency. VideoPoet demonstrates state-of-the-art video generation, in particular in producing a wide range of large, interesting, and high-fidelity motions. The VideoPoet model supports generating videos in square orientation, or portrait to tailor generations towards short-form content, as well as supporting audio generation from a video input.

[Blog Post](https://blog.research.google/2023/12/videopoet-large-language-model-for-zero.html)