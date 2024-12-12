---
post_type: "wiki" 
title: "DevContainer configurations"
last_updated_date: "06/29/2024 14:48"
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
                "ms-azuretools.vscode-docker"
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
        "ghcr.io/devcontainers-extra/features/poetry:2": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python"
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
        "ghcr.io/devcontainers-extra/features/poetry:2": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python"
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
                "Ionide.Ionide-fsharp"
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
                "Ionide.Ionide-fsharp"
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
            "version": "3.10"
        },        
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "8.0"
        }
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp"
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
            "version": "3.10"
        },        
        "ghcr.io/devcontainers/features/dotnet:2": {
            "version": "8.0"
        },
        "ghcr.io/devcontainers/features/nvidia-cuda:1": {}
    },
    "customizations": {
        "vscode": {
            "extensions": [
                "ms-vscode-remote.vscode-remote-extensionpack",
                "ms-azuretools.vscode-docker",
                "ms-python.python",
                "ms-dotnettools.csharp",
                "Ionide.Ionide-fsharp"
            ]
        }
    }
}
```

## Additional Resources

- [Developing inside a DevContainer](https://code.visualstudio.com/docs/devcontainers/containers)
- [Pre-built DevContainer images](https://github.com/devcontainers/images)
- [Pre-built DevContainer features](https://github.com/devcontainers/features)
- [GitHub Codespaces overview](https://docs.github.com/en/codespaces/overview)
- [VS Code Extensions Marketplace](https://marketplace.visualstudio.com/vscode)