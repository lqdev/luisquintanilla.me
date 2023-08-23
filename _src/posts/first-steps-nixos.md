---
post_type: "article" 
title: "First Steps with NixOS"
description: "My initial impressions of NixOS. TLDR - I really like it!"
published_date: "2023-08-22 18:03"
tags: ["nixos","linux","os","nix","sysadmin","floss"]
---

## Introduction

For the longest time, when using Linux distributions, I chose those that were Debian-based. I still do for servers. However, on the dekstop I switched to [Manjaro](https://manjaro.org/), which is Arch-based, about two years ago. Two years is a long time without distro-hopping, so I was long overdue. I've been following the [NixOS](https://nixos.org/) project for some time but since I was happy with my setup, I didn't consider making the switch. However, a month ago, I decided to [dip my toes](/feed/maybe-switching-nixos/). I installed NixOS on a flash drive and started tinkering with it. In this post, I'll provide some of my initial impressions and things I find appealing about the OS.

## Declarative configuration

One of the first things I did when I booted into my new NixOS installation was get familiar with the [configuration file](https://nixos.wiki/wiki/Overview_of_the_NixOS_Linux_distribution#Declarative_Configuration). The configuration file is authored using the [Nix programming language](https://nix.dev/tutorials/first-steps/nix-language) and it's the place where you configure your entire system, including services, packages, and desktop environments. The best way I can describe it is a Dockerfile for your desktop. If interested, you can find my [config file](/snippets/nixos-configuration/) on the scripts section of this website. 


There's a few advantages to this approach:

1. **Configurations are stored in a single place** - The advantage to this is, I don't have to figure out where the configuration files for each of the components of my system are. I can manage everything in one place. A perfect example where the configuration file goes beyond what I expected was being able to include my bash configuration. Typically, I'd have that configuration in my `bashrc` file. With NixOS, I can just include that in the configuration file as follows:


    ```nix
    programs.bash = {
        shellAliases = {
            emacs="emacs -nw";
        };
    };
    ```

1. **Enables composition** - Because the configuration file is effectively a script, I can modularize the difference pieces. For example, if I wanted to split out my service and package configurations into separate files, I can do so and reference those individual files from my configuration file, separating concerns while still keeping my system configuration simple. 
1. **Version control** - Because the configuration file is just like any other plain text file, it means I can check it into the version control system or cloud service of my choice and manage it that way. In doing so, not only can I roll back using the built-in NixOS mechanisms, but I have another layer of security in case I need to recover that file. 

Now, because I can define everything about my system in the configuration file, this means I can seamlessly rebuild my entire system using this single file in a reproducible manner.

## Reproducible builds

I got to experience first-hand how the configuration file can simplify out-of-box-experiences and system configurations. Initially, I did not install NixOS on my main hard drive. All of my configuration and learning took place on a flash drive. Once I got my configuration to a place I was satisfied, it was time to take the plunge and reimage my PC. In the past, when I've done something similar, I've had to document everything I did when configuring my system. That's where posts like [Setting up a new Ubuntu PC](/posts/setting-up-new-ubuntu-pc/) come from. With NixOS, all I needed to do was replace my configuration file with the one I configured on the flash drive. Then, when I ran `nixos-rebuild switch`, my system was configured exactly like the one on the flash drive. 

## Risk-free software evaluations

This is something I haven't tried yet, but I could easily see it coming in handy. Sometimes I might need a piece of software to do one thing or maybe I want to see whether it'll solve a problem I'm facing. In order to try it out and use it, I need to install it globally. This can cause changes to my system permanently that I don't want. With NixOS, you can create [ad-hoc shell environments](https://nix.dev/tutorials/first-steps/ad-hoc-shell-environments#ad-hoc-shell-environments). These environments make temporary changes to your system, so you can evaluate a piece of software or use it that one time. Then, when you exit the environment, the software is no longer installed in your system.

## Large package selection

A [large package selection](https://search.nixos.org/packages) to choose from is something I got used to with Manjaro. Being an Arch-based distribution, I had access to the [Arch User Repository (AUR)](https://aur.archlinux.org/). Moving to NixOS, it's nice to know that in this area, NixOS offers great support. Additionally, NixOS has built-in support for [Flatpak](https://nixos.wiki/wiki/Flatpak)

## Still exploring

There's still a few things I don't really get. For example, when installing the .NET SDK, I wanted to have side-by-side installs of the latest STS (7.0) and LTS (6.0). However, including the package names in my configuration file didn't work as expected. Instead I had to use the following convention.

```nix
(with dotnetCorePackages; combinePackages [
    dotnet-sdk
    dotnet-sdk_7
])
```

As I learn more, this will probably make more sense but for now it's a mystery. 

## Conclusion

Overall, I'm happy with NixOS and I can see myself using it for the long-term. Hopefully longer than two years. I'm not a gamer so I can't say how well it does in that front. For web browsing and software development though, I really like what NixOS has to offer. If some of the things I mentioned above sound interesting, I'd encourage you to install it on a flash drive and start tinkering with it. 