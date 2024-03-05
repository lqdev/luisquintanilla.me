---
post_type: "article" 
title: "Getting started with Ollama on Windows"
description: "Start using Generative AI models like Llama and Phi locally on your Windows PC with Ollama"
published_date: "2024-03-05 10:32"
tags: ["ai","ollama","windows","llm","opensource","llama","openai","generativeai","genai"]
---

Recently [Ollama announced support for Windows](/feed/ollama-windows-preview) in preview. In doing so, people who want to use AI models like Llama, Phi, and many others can do so locally on their PC. In this post, I'll go over how you can get started with Ollama on Windows. 

## Install Ollama

The first thing you'll want to do is install Ollama.

You can do so by [downloading the installer from the website](https://ollama.com/download/windows) and following the installation prompts. 

## Get a model

Once you've installed Ollama, it's time to get a model.

1. Open PowerShell
1. Run the following command

    ```powershell
    ollama pull llama2
    ```

In this case, I'm using llama2. However, you can choose another model. You could even download many models at once and switch between them. For a full list of supported models, see the [Ollama model documentation](https://ollama.com/library).

## Use the model

Now that you have the model, it's time to use it. The easiest way to use the model is using the REST API. When you install Ollama, it starts up a server to host your model. One other neat thing is, the REST API is [OpenAI API compatible](https://ollama.com/blog/openai-compatibility).

1. Open PowerShell
1. Send the following request:

    ```powershell
    (Invoke-WebRequest -method POST -Body '{"model":"llama2", "prompt":"Why is the sky blue?", "stream": false}' -uri http://localhost:11434/api/generate ).Content | ConvertFrom-json
    ```

    This command will issue an HTTP POST request to the server listening on port 11434.

    The main things to highlight in the body:

    - *model*: The model you'll use. Make sure this is one of the models you pulled. 
    - *prompt*: The input to the model
    - *stream*: Whether to stream responses back to the client

    For more details on the REST API, see the [Ollama REST API documentation](https://github.com/ollama/ollama/blob/main/docs/api.md). 

## Conclusion

In this post, I went over how you can quickly install Ollama to start using generative AI models like Llama and Phi locally on your Windows PC. If you use Mac or Linux, you can perform similar steps as those outlined in this guide to get started on those operating systems. Happy coding! 