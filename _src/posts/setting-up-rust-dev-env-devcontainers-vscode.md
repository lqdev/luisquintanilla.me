---
post_type: "article" 
title: "Setting up your Rust development environment using Dev Containers"
description: "Configure Dev Containers for full-featured development environments that can be used locally or in the cloud with Docker, VS Code, and GitHub Codespaces"
published_date: "2024-01-31 21:05"
tags: ["rust","vscode","devcontainers","codespaces","programming","technology","docker","containers","github"]
---

Setting up a new development environment that's configured with all the SDKs and tools you need to get started working can be a multi-step process that is error-prone. Especially when you don't know whether you want to commit to a specific technology or don't have too much space on your device, you might not want to install everything on your device. Additionally, cleaning up and removing everything you installed isn't always 100%. This is where development environments like Dev Containers can come in handy. 

## What are Dev Containers

A development container (Dev Container) lets you use a container as a full development environment. 

Because your development environment is in a container, the environment is isolated. The builds are reproducible and disposable. This means you can quickly stand up a new clean development environments and dispose of them just as easily. 

For more details, see the [Dev Containers website](https://containers.dev/).

If you have Docker installed, you can run them locally. Otherwise, you can run Dev Containers using GitHub Codespaces

## What are GitHub Codespaces

A codespace is a development environment that's hosted in the cloud.

For more details, see the [GitHub Codespaces website](https://github.com/features/codespaces).

## Set up Dev Container for Rust Development

1. Create a new directory.
1. Add a Dev Container configuration file called *.devcontainer.json* that contains the following content:

    ```json
    {
        "name": "Rust Development Environment",
        "image": "mcr.microsoft.com/devcontainers/rust:latest",
        "customizations": {
            "vscode": {
                "extensions": [
                    "rust-lang.rust-analyzer"
                ]
            }
        }
    }
    ```

    - **name**: A human friendly name to identify your development environment. It's not as important when you only have one environment but if you have different environments, it can be useful for differentiating between the various configurations. 
    - **image**: For the most customization, you can provide the path to a Docker file. However, to make things easier, there are a set of prebuilt container images. In this case, I'm using the [Rust image](https://github.com/devcontainers/images/tree/main/src/rust).
    - **customizations.vscode.extensions**: A list of Visual Studio Code extensions that can help you with your development. In this case, it's the [*rust-analyzer*](https://marketplace.visualstudio.com/items?itemName=rust-lang.rust-analyzer).

At minimum, this is all you need to get started. 

## Start your environment 

[Visual Studio Code provides good support for Dev Containers](https://code.visualstudio.com/docs/devcontainers/tutorial). 

1. Install [VS Code](https://code.visualstudio.com/download)
1. Open your directory where you configured your Dev Container in Visual Studio Code
1. Install the [Dev Containers VS Code extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
1. Open the command palette in VS Code. In the menu bar, select **View > Command Palette.**
1. Enter the following command **>Dev Containers: Open Dev Container**. This will start building your dev container. Wait for it to start.

## Check your environment

Once your Dev Container starts, check that `rustc` is installed.

1. Open the terminal.
1. Run `rustc -h`. A help message should output to the console for the Rust compiler. 

## Build your app

For simplicity, I've used the [Hello World sample from the Rust By Example website](https://doc.rust-lang.org/rust-by-example/hello.html).

1. Create a new file called *hello.rs*.
1. Add the following code:

    ```rust
    fn main ()
    {
        println!("Hello World!");
    }
    ```
1. Compile your program

    ```bash
    rustc hello.rs
    ```

    This will generate an executable called *hello*

1. Run your app

    ```bash
    ./hello
    ```

    The application should print out the string "Hello World" to the console. 

That's it! You now have a Rust development environment to get started learning and building apps. 

## Conclusion

Dev Containers make it easy for you to configure full-featured development environments. When paired with Visual Studio Code and GitHub Codespaces, they can help you focus on learning and building applications rather than setting up your environment. You can find a [final version of the configured environment and app on GitHub](https://github.com/lqdev/rust-codespace-sandbox). Happy coding! 

## Additional Resources

- [Rust in Visual Studio Code](https://code.visualstudio.com/docs/languages/rust)
- [Rust by Example](https://doc.rust-lang.org/rust-by-example/index.html)
- [Developing Inside a Container](https://code.visualstudio.com/docs/devcontainers/containers)