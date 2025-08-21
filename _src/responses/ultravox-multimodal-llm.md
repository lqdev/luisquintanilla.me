---
title: "Ultravox - An open, fast, and extensible multimodal LLM"
targeturl: https://github.com/fixie-ai/ultravox
response_type: reshare
dt_published: "2024-06-11 20:51 -05:00"
dt_updated: "2024-06-11 20:51 -05:00"
tags: ["ai","multimodal","llm"]
---

> Ultravox is a new kind of multimodal LLM that can understand text as well as human speech, without the need for a separate Audio Speech Recognition (ASR) stage. Building on research like AudioLM, SeamlessM4T, Gazelle, SpeechGPT, and others, we've extended Meta's Llama 3 model with a multimodal projector that converts audio directly into the high-dimensional space used by Llama 3. This direct coupling allows Ultravox to respond much more quickly than systems that combine separate ASR and LLM components. In the future this will also allow Ultravox to natively understand the paralinguistic cues of timing and emotion that are omnipresent in human speech.  
> <br>
> The current version of Ultravox (v0.1), when invoked with audio content, has a time-to-first-token (TTFT) of approximately 200ms, and a tokens-per-second rate of ~100, all using a Llama 3 8B backbone. While quite fast, we believe there is considerable room for improvement in these numbers. We look forward to working with LLM hosting providers to deliver state-of-the-art performance for Ultravox.  
> <br>
> Ultravox currently takes in audio and emits streaming text. As we evolve the model, we'll train it to be able to emit a stream of speech tokens that can then be converted directly into raw audio by an appropriate unit vocoder. We're interested in working with interested parties to build this functionality!