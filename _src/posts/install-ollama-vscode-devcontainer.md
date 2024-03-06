---
post_type: "article" 
title: "Configure Ollama on Dev Containers and VS Code"
description: "Set up Ollama on your VS Code development environments and get started using AI models locally in your projects"
published_date: "2024-03-06 13:50"
tags: ["ollama","vscode","devcontainer","ai","llm","opensource","development"]
---

The source for my website is [hosted on GitHub](/github/luisquintanilla.me). As a result, I use VS Code as the text editor to author my posts. Typically, I use [Codespaces or github.dev](/colophon) to quickly draft and publish articles to simplify the process. 

As part of my authoring and publishing posts, there's a few things I do like:

1. Coming up with a title
2. Creating relevant tags to help with discovery and connecting related information on my website.
3. For long-form blog posts, I also include a description. 

This is generally relatively easy to do. However, sometimes I spend too much time coming up with something that truly summarizes and condenses the information in the post. It'd be nice if I had an assistant to help me brainstorm and workshop some of these items.

In comes AI. Now, I could use something like [Copilot](https://code.visualstudio.com/docs/copilot/overview) which would work wonderfully and easily plug into my workflow. However, my website is a labor of love and I don't make any money from it. In many instances, I've designed [various components to be as low-cost as possible](/posts/receive-webmentions-fsharp-az-functions-fsadvent). 

Recently, I created a post which showed [how to get started with Ollama on Windows](/posts/getting-started-ollama-windows). In this post, I'll show how you can do the same for your Dev Container environments. 

## Install Ollama

Assuming you already have a [Dev Container configuration file](https://code.visualstudio.com/docs/devcontainers/create-dev-container), add the following line to it:

```json
"postCreateCommand": "curl -fsSL https://ollama.com/install.sh | sh"
```

This command triggers when the container environment is created. This will install Ollama in your development environment. For more details, see the [Ollama download instructions](https://ollama.com/download/linux).

## Start Ollama

Now that Ollama is installed, it's time to start the service so you can get models and use them. To start the Ollama service on your Dev Conatiner environment, add the following line to your Dev Container configuration file.

```json
"postStartCommand": "ollama serve"
```

This command will run `ollama serve` when the Dev Container environment starts.

## Pull a model

To use Ollama, you're going to need a model. In my case, I went with [Phi-2](https://ollama.com/library/phi) because it's lightweight and space on Codespaces is limited.  

To get the model:

1. Open the terminal
1. Enter the following command:

    ```bash
    ollama pull phi
    ```

    After a few minutes, the model is downloaded and ready to use. 

1. Enter the following command to ensure that your model is running

    ```bash
    ollama list
    ```

For more details, see the [Ollama model library](https://ollama.com/library). 

## Conclusion

In this post, I showed how you can configure Ollama with Dev Containers to use AI models locally in your projects. In subsequent posts, I'll show how once the service is running, I use it as part of my authoring and publishing posts. You can see a preview of it in my [scripts directory](https://github.com/lqdev/luisquintanilla.me/blob/main/Scripts/ai.fsx). Happy coding! 