---
post_type: "wiki" 
title: "Running Waydroid on NixOS using X11"
last_updated_date: "10/05/2023 22:25"
tags: android,linux,nixos,x11,waydroid
---

## Overview

## Packages

- [weston](https://search.nixos.org/packages?channel=23.05&show=weston&from=0&size=50&sort=relevance&type=packages&query=weston) - Wayland compositor

## NixOS Configuration

```nix
virtualisation = {
    waydroid = {
        enable = true;
    };
};

environment.systemPackages = with pkgs; [weston];
```

### Initialize Waydroid

This downloads the LineageOS image and installs it

```bash
# Fetch WayDroid images.
# You can add the parameters "-s GAPPS -f" to have GApps support.
sudo waydroid init
```

### Start Waydroid


1. Open the terminal and start the waydroid container

    ```bash
    sudo systemctl start waydroid-container
    ```

1. Check status of container

    ```bash
    sudo journalctl -u waydroid-container
    ```

1. Open terminal and start weston

    ```bash
    weston
    ```

1. In the weston window, open a terminal and start Waydroid sesson in the background

    ```bash
    waydroid session start &
    ```

1. Start the UI

    ```bash
    waydroid show-full-ui
    ```

### Stop Waydroid

1. Close weston window
1. Stop waydroid container

    ```bash
    sudo systemctl stop waydroid-container
    ```

#### Remove user data

Only do this if you don't want to persist anything from that container

```bash
# Removing images and user data
sudo rm -r /var/lib/waydroid/* ~/.local/share/waydroid
```


### Update Android

```bash
sudo waydroid upgrade
```

### General Usage

```bash
# Start Android UI
waydroid show-full-ui

# List Android apps
waydroid app list

# Start an Android app
waydroid app launch <application name>

# Install an Android app
waydroid app install </path/to/app.apk>

# Enter the LXC shell
sudo waydroid shell

# Overrides the full-ui width
waydroid prop set persist.waydroid.width 608
```

## Resources

- [NixOS Wiki - WayDroid](https://nixos.wiki/wiki/WayDroid)
- [Script to start LineageOS on X11](https://unix.stackexchange.com/questions/732485/script-to-start-android-lineageos-with-waydroid-in-an-x11-session)
- [Any plan to support X11?](https://github.com/waydroid/waydroid/issues/195#issuecomment-953926526)