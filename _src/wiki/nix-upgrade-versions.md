---
post_type: "wiki"
title: Upgrade NixOS versions
last_updated_date: 2024-07-14 14:49 -05:00
tags: linux,sysadmin,nixos,nix,os
---

## Overview

This guide provides general guidance on upgrading between NixOS versions

## Check which version you're currently running

```bash
cat /etc/lsb_release
```

## Get list of channels

This provides the URL used to download packages for nixos release

```bash
nix-channel --list | grep nixos
```

## Add / Replace Software Channel

To get on the latest version, you need to update the `nixos` channel to the latest version.

You can find a list of versions in this [repository](https://channels.nixos.org/).

For example, if you wanted to upgrade to the latest 24.05 version, you'd use the following command:

```bash
nix-channel --add https://channels.nixos.org/nixos-24.05 nixos
```

The general format is: `nix-channel --add <CHANNEL_URL> nixos`

## Rebuild your system

Once you've configured the channel for the latest version, switch to it just like you would when upgading sofware packages.

```bash
nixos-rebuild switch --upgrade
```

After the operation completes, you'll want to check which version you have using the lsb_release instructions above.

## Additional Resources

- [Upgrading Nix OS](https://nlewo.github.io/nixos-manual-sphinx/installation/upgrading.xml.html)
