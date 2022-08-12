---
title: "Download Main Wikipedia Page as PDF"
language: "Bash"
tags: bash,linux
---

## Description

Download the main page of Wikipedia locally as a PDF

## Usage

```bash
curl -o Main_Page.pdf https://en.wikipedia.org/api/rest_v1/page/pdf/Main_Page
```

## Snippet

### script.sh

```bash
curl -o Main_Page.pdf https://en.wikipedia.org/api/rest_v1/page/pdf/Main_Page
```