---
post_type: "wiki" 
title: "Garbage Collections NixOS"
last_updated_date: "03/10/2024 14:01 -05:00"
tags: nixos,system,os,linux
---

## Overview

One of the nice things about NixOS is, whenever you update the system, old configurations remain in place. That way if something were to go wrong, you can always revert back. However, there are tradeoffs. Each old configuration takes up space on your hard drive. If you're updating your system every week or every few weeks, this adds up. The following is a general guide on how to clean these up.

## Delete old generations

This command deletes every generation, except the most current one. 

```bash
nix-env --delete-generations old
```

If you'd like to keep a few of the most recent ones. Say, from the last 14 days, you can pass an argument.

```bash
nix-env --delete-generations 14d
```

## Run the garbage collector

```bash
nix-store --gc
```

## Use the nix-collect-garbage utility

The following utility will delete older generations across all profiles on your system. 

```bash
nix-collect-garbage -d
```

## Resources

[NixOS Garbage Collection](https://nixos.org/manual/nix/stable/package-management/garbage-collection.html)