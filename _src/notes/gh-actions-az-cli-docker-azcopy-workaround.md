---
post_type: "note" 
title: "Workaround for Azure CLI docker image azcopy issue in GitHub Actions"
published_date: "2025-01-10 21:12 -05:00"
tags: ["azure","github","cicd"]
---

Publishing on my site was broken since yesterday. I thought it was only me but after doing some digging looks like it was an issue with Azure CLI and the link it uses to download azcopy.

Looks like the team is already working on a fix and you can track progress using this issue

[https://github.com/Azure/azure-cli/issues/30635](https://github.com/Azure/azure-cli/issues/30635)

If you're using Azure CLI in GitHub Actions and running into this problem, you can unblock yourself by installing azcopy manually. Here's the snippet that worked for me. 

```yaml
- name: Upload to blob storage
  uses: azure/CLI@v1
  with:
    azcliversion: 2.67.0
    # azcopy workadound https://github.com/Azure/azure-cli/issues/30635
    inlineScript: |
        tdnf install -y azcopy;
        # YOUR SCRIPT
```

