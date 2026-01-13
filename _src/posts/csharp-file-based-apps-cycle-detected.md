---
post_type: "article" 
title: "Cycle Detected in C# File Based Apps"
description: "What to do if you run into cycle detected errors in C# file-based apps"
published_date: "2026-01-05 23:19 -05:00"
tags: ["csharp","dotnet","programming"]
---

Today I Learned you have to be mindful of how you name your [C# file-based app](https://learn.microsoft.com/dotnet/csharp/fundamentals/tutorials/file-based-programs) files.

Earlier today I tried running the following file-based app which I'd named *OpenAI.cs*.

```csharp
#:package Microsoft.Extensions.AI.OpenAI@10.1.1-preview.1.25612.2

using Microsoft.Extensions.AI;
using OpenAI;

var key = Environment.GetEnvironmentVariable("OPENAI_KEY");

IChatClient chatClient = 
    new OpenAIClient(key)
        .GetChatClient("gpt-4o-mini")
        .AsIChatClient();

var res = await chatClient.GetResponseAsync("What is AI?");

Console.WriteLine(res);
```

That threw the following error

```text
error NU1108:
  Cycle detected.
    OpenAI -> Microsoft.Extensions.AI.OpenAI 10.1.1-preview.1.25612.2 -> OpenAI (>= 2.8.0).

The build failed. Fix the build errors and run again.
```

The reason for it is, my app is using [OpenAI NuGet package](https://www.nuget.org/packages/OpenAI) as a dependency. So that package is clashing with my *OpenAI.cs* file. 

Renaming the file to something other than *OpenAI.cs* (i.e. *OpenAISample.cs*) fixed it.

Hopefully this helps if you run into a similar issue.