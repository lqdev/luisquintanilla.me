---
post_type: "wiki" 
title: "DevContainer configurations"
last_updated_date: "06/29/2024 14:48 -05:00"
tags: devcontainer,vscode,codespaces,development,software,tech,programming,python,dotnet,csharp,fsharp,docker,git,debian
---

## Overview

A collection of DevContainer configurations

## Base Debian Image

```json
{
    "name": "lqdev.me Base Debian DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev"
            ]
        }
    }
}
```

## Python

```json
{
    "name": "lqdev.me Python DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/python:1": {
            "version": "3.11"
        },
        "ghcr.io/va-h/devcontainers-features/uv:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev"                
            ]
        }
    }
}
```

## Python (GPU)

```json
{
    "name": "lqdev.me Python (GPU) DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/python:1": {
            "version": "3.11"
        },
        "ghcr.io/devcontainers/features/nvidia-cuda:1": {},
        "ghcr.io/va-h/devcontainers-features/uv:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev"                
            ]
        }
    },
    "runArgs": [
        "--gpus", 
        "all"
    ]
}
```

## .NET

```json
{
    "name": "lqdev.me .NET DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "9.0"
        }
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev",
                "ms-dotnettools.csdevkit"                                
            ]
        }
    }
}
```

## .NET (GPU)

```json
{
    "name": "lqdev.me .NET (GPU) DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "9.0"
        },
        "ghcr.io/devcontainers/features/nvidia-cuda:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev",
                "ms-dotnettools.csdevkit"                
            ]
        }
    },
    "runArgs": [
        "--gpus", 
        "all"
    ]
}
```

## Python and .NET

```json
{
    "name": "lqdev.me Python and .NET DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/python:1": {
            "version": "3.11"
        },        
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "9.0"
        },
        "ghcr.io/va-h/devcontainers-features/uv:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev",
                "ms-dotnettools.csdevkit"                
            ]
        }
    }
}
```

## Python and .NET (GPU)

```json
{
    "name": "lqdev.me Python and .NET (GPU) DevContainer",
    "image": "mcr.microsoft.com/devcontainers/base:debian",
    "features": {
        "ghcr.io/devcontainers/features/git:1": {},
        "ghcr.io/devcontainers/features/docker-in-docker:2": {},
        "ghcr.io/devcontainers/features/python:1": {
            "version": "3.11"
        },        
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "9.0"
        },
        "ghcr.io/devcontainers/features/nvidia-cuda:1": {},
        "ghcr.io/va-h/devcontainers-features/uv:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp",
                "GitHub.copilot",
                "GitHub.copilot-chat",
                "saoudrizwan.claude-dev",
                "ms-dotnettools.csdevkit"               
            ]
        }
    },
    "runArgs": [
        "--gpus", 
        "all"
    ]
}
```

## Additional Resources

- [Developing inside a DevContainer](https://code.visualstudio.com/docs/devcontainers/containers)
- [Pre-built DevContainer images](https://github.com/devcontainers/images)
- [Pre-built DevContainer features](https://github.com/devcontainers/features)
- [GitHub Codespaces overview](https://docs.github.com/en/codespaces/overview)
- [VS Code Extensions Marketplace](https://marketplace.visualstudio.com/vscode)