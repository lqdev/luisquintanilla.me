---
title: "Create a new Matrix user using the CLI"
language: "bash"
tags: bash,linux,matrix,synapse,communication
---

## Description

Create a new user in your [Matrix](https://matrix.org/) [Synapse homeserver](https://github.com/matrix-org/synapse/) using [register_new_matrix_user](https://manpages.debian.org/buster/matrix-synapse/register_new_matrix_user.1.en.html) CLI utility. This is helpful when creating accounts on a homeserver where registrations are closeds.

## Usage

N/A

## Snippet

```bash
register_new_matrix_user --user user1 --password p@ssword --config homeserver-config.yaml
```

- **--user**: Local part of the new user. Will prompt if omitted.
- **--password**: New password for user. Will prompt if omitted. Supplying the password on the command line is not recommended. Use the STDIN instead.
- **--config**: Path to server config file containing the shared secret.