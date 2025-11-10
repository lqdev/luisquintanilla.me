---
title: "There is no such thing as a tokenizer-free lunch"
targeturl: https://huggingface.co/blog/catherinearnett/in-defense-of-tokenizers
response_type: reshare
dt_published: "2025-11-09 22:59 -05:00"
dt_updated: "2025-11-09 22:59 -05:00"
tags: ["tokenizers","hugging face","ai","nlp"]
---

> The only time most people hear about tokenization is when it’s being blamed for some undesirable behavior of a language model. These incidents have helped turn ignorance and indifference towards tokenizers into active dismissal and disdain. This attitude makes it harder to understand the tokenizers and develop better ones, because fewer people are actually studying tokenizers.  
> <br>
> The goal of this blog post is to provide some context about how we got the tokenization approaches we have and argue that they’re not actually so bad.  
> <br>
> On a personal level, I also want to foster more engagement with the tokenization literature. Regardless of whether you are pro- or anti-tokenization, more people to be working on issues related to tokenizers, the faster we’re going to make progress. For those that think they are taking a “tokenizer-free” approach, I argue that these approaches are just other kinds of tokenization. And incorporating the findings from static subword tokenization research can only help develop better alternatives.