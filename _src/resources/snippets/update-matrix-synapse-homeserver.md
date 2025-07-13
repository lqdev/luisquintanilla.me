---
title: "Upgrade Matrix Synapse homeserver"
language: "bash"
tags: matrix,synapse,homeserver,selfhost,python,internet,network
---

## Description

Upgrade a [Matrix](https://matrix.org/) [Synapse homeserver](https://github.com/matrix-org/synapse/) using pip. For more information, see the official article on [upgrading between Synapse versions](https://matrix-org.github.io/synapse/develop/upgrade).

## Usage

```bash
./update-matrix-homeserver.sh
```

## Snippet

### update-matrix-homeserver.sh

```bash
# Initialize Python virtual environment
source ./env/bin/activate

# Upgrade using pip
# For PostgreSQL packages, use matrix-synapse[postgres]
pip install --upgrade matrix-synapse

# Restart server
synctl restart

# Check version
curl http://localhost:8008/_synapse/admin/v1/server_version

# Deactivate Python virtual environment
deactivate
```