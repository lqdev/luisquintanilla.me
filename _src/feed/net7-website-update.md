---
post_type: "note" 
title: "Upgraded website to .NET 7"
published_date: "2022-11-11 09:59"
tags: ["dotnet","website","net7","upgrade"]
---

Last year, I posted about [upgrading this website to .NET 6](/feed/net6-website-update) and how easy that process was. I just had a chance to upgrade to .NET 7 and the experience was just as smooth. All I had to do was change the version in two places:

**GitHub Actions \*.yml file**

```yml
dotnet-version: '7.0.x'
```

**\*.fsproj  file**

```xml
<TargetFramework>net7.0</TargetFramework>
```

I'm happy that with such a simple change, I can get all the benefits of .NET 7 without doing a lot of work. 