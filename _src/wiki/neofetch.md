---
post_type: "wiki" 
title: "Neofetch"
last_updated_date: "08/10/2022 21:38"
tags: technology, linux, tools, terminal
---

## What is NeoFetch?

Neofetch is a command-line system information tool. Neofetch displays information about your operating system, software and hardware.

## Install using Pacman

1. Open the terminal
1. Run the following command

```bash
pacman -S neofetch
```

## Configure

The configuration file for neofetch is located at *$HOME/.config/neofetch/config.conf*

## Run

1. Open the terminal and enter the following command:

```bash
neofetch
```

## Run when terminal launches 

1. Open your *~/.bashrc* file in your preferred text editor.
1. Add the following line to your file and save it.

    ```bash
    neofetch
    ```

1. Refresh your terminal with the following command.

    ```bash
    source ~/.bashrc
    ```

## References

- [Neofetch GitHub Repository](https://github.com/dylanaraps/neofetch)