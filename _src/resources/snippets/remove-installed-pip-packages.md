---
title: "Remove all installed Python packages with pip"
language: "Bash"
tags: python,pip,packages 
created_date: "08/08/2024 10:26 -05:00"
---

## Description

I recently had the need to get rid of all the packages I'd installed due to conflicting dependencies. 

## Usage

To uninstall packages you need to:

1. Get a list of the packages
2. Uninstall them

This works both for virtual environments as well as system-wide installations.

### Get all packages

```bash
pip freeze > requirements.txt
```

### Uninstall packages

```bash
pip uninstall -r requirements.txt -y
```

## Snippet

N/A