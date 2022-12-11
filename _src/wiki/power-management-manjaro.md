---
post_type: "wiki" 
title: "Power Management Manjaro"
last_updated_date: "12/11/2022 16:15"
tags: manjaro, bash, powermanagement, tech, wiki, linux
---

## Overview

The following shows how you can optimize your computer using Powertop to generate runing recommendations and apply those optimizations at boot using an rc-local service.

## Powertop

1. Install powertop

    ```bash
    sudo pacman -S powertop
    ```

1. Run powertop report (on battery power)

    ```
    sudo powertop -r
    ```

    After a few seconds, a file called *powertop.html* is created. 

1. Open the *powertop.html* file in your browser and inspect the **Tuning** tab for tuning recommendations. 

## Configure rc-local service

1. Create *rc.local* file in `/etc` directory with the following content

    ```bash
    #!/bin/sh -e

    echo "Hello World"

    # Insert powertop optimization scripts here. 

    exit 0
    ```

1. Create *rc-local.service* file in `/etc/systemd/system` directory with the following content

    ```bash
    #  This file is part of systemd.
    #
    #  systemd is free software; you can redistribute it and/or modify it
    #  under the terms of the GNU General Public License as published by
    #  the Free Software Foundation; either version 2 of the License, or
    #  (at your option) any later version.

    [Unit]
    Description=/etc/rc.local Compatibility
    ConditionPathExists=/etc/rc.local

    [Service]
    Type=oneshot
    ExecStart=/etc/rc.local
    TimeoutSec=0
    StandardOutput=tty
    RemainAfterExit=yes
    SysVStartPriority=99

    [Install]
    WantedBy=multi-user.target
    ```

1. Enable and start rc-local service

    ```bash
    sudo systemctl enable rc-local
    sydo systemctl start rc-local
    ```

1. Confirm that the service has successfully started

    ```bash
    sudo systemctl status rc-local
    ```

## References

- [Arch Wiki - Powertop](https://wiki.archlinux.org/title/Powertop)