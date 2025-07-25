---
title: "Download Main Wikipedia Page as PDF"
language: "bash"
tags: bash,linux
created_date: "08/11/2022 19:19 -05:00"
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