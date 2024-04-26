---
post_type: "wiki" 
title: "Pandoc"
last_updated_date: "04/25/2024 22:00"
tags: pandoc, plaintext, docx, documents, word, org, emacs, utility, linux
---

## Overview

If you need to convert files from one markup format into another, pandoc is your swiss-army knife.

- [Website](https://pandoc.org/)

## Recipes

- [List of recipes](https://pandoc.org/demos.html)

### Emacs org-mode to Microsoft Word

Personally, I like drafting and structuring documents in org-mode. However, for sharing and collaboration, that usually takes place in Microsoft Word. This is a command I use on a regular basis.

```bash
pandoc -s -o <document-name>.docx <document-name>.org
```