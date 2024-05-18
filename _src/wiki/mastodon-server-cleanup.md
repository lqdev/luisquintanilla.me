---
post_type: "wiki" 
title: "Mastodon Server Cleanup"
last_updated_date: "08/25/2023 16:04"
tags: mastodon,linux,socialmedia,sysadmin
---

## Overview

General commands for cleaning up resources on Mastodon servers

## Setup

1. Stop services

    ```bash
    sudo systemctl stop mastodon-sidekiq mastodon-streaming mastodon-web
    ```

1. Restart postgresql

    ```bash
    sudo systemctl restart postgresql
    ```

1. Log into mastodon user

    ```bash
    sudo su -u mastodon
    ```

1. Go to `live` directory

    ```bash
    cd /home/mastodon/live
    ```

## Check media usage

```bash
RAILS_ENV=production ./bin/tootctl media usage
```

## Remove media

```bash
RAILS_ENV=production ./bin/tootctl media remove
```

## Remove media headers

```bash
RAILS_ENV=production ./bin/tootctl media remove --prune-profiles
```

## Remove preview cards

```bash
RAILS_ENV=production ./bin/tootctl preview_cards remove
```

## Restart services

```bash
sudo systemctl restart mastodon-sidekiq mastodon-streaming mastodon-web
```

## Resources

- [Using the Admin CLI](https://docs.joinmastodon.org/admin/tootctl/)