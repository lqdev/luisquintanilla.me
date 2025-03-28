---
title: "Winget Configuration"
language: "Yaml"
tags: windows,powershell ,yaml
---

## Description

My Winget Configuration file

## Usage

```powershell
winget configure -f <FILENAME>.dsc
```

## Snippet

```yaml
# yaml-language-server: $schema=https://aka.ms/configuration-dsc-schema/0.2
# Reference: https://github.com/microsoft/winget-create#building-the-client
# WinGet Configure file Generated By Dev Home.

properties:
  resources:
  - resource: Microsoft.Windows.Developer/DeveloperMode
    directives:
      description: Enable Developer Mode
      allowPrerelease: true
    settings:
      Ensure: Present  
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.VisualStudio.2022.Community
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.VisualStudio.2022.Community"
      source: winget
    id: Microsoft.VisualStudio.2022.Community
  - resource: Microsoft.VisualStudio.DSC/VSComponents
    dependsOn:
      - Microsoft.VisualStudio.2022.Community
    directives:
      description: Install required VS workloads
      allowPrerelease: true
    settings:
      productId: Microsoft.VisualStudio.Product.Community
      channelId: VisualStudio.17.Release
      components:
        - Microsoft.VisualStudio.Workload.Azure
        - Microsoft.VisualStudio.Workload.NetWeb
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.VisualStudioCode
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.VisualStudioCode"
      source: winget
    id: Microsoft.VisualStudioCode
  #   https://github.com/microsoft/winget-cli/discussions/3958
  # - resource: vscode/VSCodeExtension
  #   directives:
  #     description: Install Remote Development Extension
  #     allowPrerelease: true
  #   settings:
  #     Name: ms-vscode-remote.vscode-remote-extensionpack
  #     Ensure: Present
  # - resource: vscode/VSCodeExtension
  #   directives:
  #     description: Install YAML Extension
  #     allowPrerelease: true
  #   settings:
  #     Name: redhat.vscode-yaml
  #     Ensure: Present
  # - resource: vscode/VSCodeExtension
  #   directives:
  #     description: Install Ionide Extension
  #     allowPrerelease: true
  #   settings:
  #     Name: Ionide.Ionide-fsharp
  #     Ensure: Present
  # - resource: vscode/VSCodeExtension
  #   directives:
  #     description: Install C# Extension
  #     allowPrerelease: true
  #   settings:
  #     Name: ms-dotnettools.csdevkit
  #     Ensure: Present      
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Git.Git
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Git.Git"
      source: winget
    id: Git.Git
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.PowerShell
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.PowerShell"
      source: winget
    id: Microsoft.PowerShell
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Docker.DockerDesktop
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Docker.DockerDesktop"
      source: winget
    id: Docker.DockerDesktop
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Debian.Debian
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Debian.Debian"
      source: winget
    id: Debian.Debian
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.DotNet.SDK.8
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.DotNet.SDK.8"
      source: winget
    id: Microsoft.DotNet.SDK.8
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.DotNet.SDK.9
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.DotNet.SDK.9"
      source: winget
    id: Microsoft.DotNet.SDK.9
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing OBSProject.OBSStudio
      allowPrerelease: true
      securityContext: current
    settings:
      id: "OBSProject.OBSStudio"
      source: winget
    id: OBSProject.OBSStudio
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.WSL
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Microsoft.WSL"
      source: winget
    id: Microsoft.WSL
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Ollama.Ollama
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Ollama.Ollama"
      source: winget
    id: Ollama.Ollama
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Microsoft.WindowsTerminal
      allowPrerelease: false
      securityContext: current
    settings:
      id: "Microsoft.WindowsTerminal"
      source: winget
    id: Microsoft.WindowsTerminal
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Brave Browser
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Brave.Brave"
      source: winget
    id: Brave.Brave    
  # https://github.com/microsoft/winget-pkgs/issues/155070
  # - resource: Microsoft.WinGet.DSC/WinGetPackage
  #   directives:
  #     description: Installing NordVPN
  #     allowPrerelease: true
  #     securityContext: current
  #   settings:
  #     id: "NordSecurity.NordVPN"
  #     source: winget
  #   id: NordSecurity.NordVPN
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Thunderbird
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Mozilla.Thunderbird"
      source: winget
    id: Mozilla.Thunderbird  
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing ProtonMail
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Proton.ProtonMail"
      source: winget
    id: Proton.ProtonMail     
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing Bitwarden
      allowPrerelease: true
      securityContext: current
    settings:
      id: "Bitwarden.Bitwarden"
      source: winget
    id: Bitwarden.Bitwarden    
  - resource: Microsoft.WinGet.DSC/WinGetPackage
    directives:
      description: Installing VLC
      allowPrerelease: true
      securityContext: current
    settings:
      id: "VideoLAN.VLC"
      source: winget
    id: VideoLAN.VLC        
  configurationVersion: 0.2.0
```