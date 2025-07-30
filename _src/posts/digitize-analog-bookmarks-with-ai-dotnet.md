---
post_type: "article" 
title: "Digitize Analog Bookmarks using AI, .NET, and GitHub Models"
description: "Use .NET and AI models like GPT-4o to extract annotated passages from an image"
published_date: "2024-10-31 21:41 -05:00"
tags: ["ai","dotnet","github","reading","bookmarks","analog","digital","reading","notes"]
---

## Introduction

This past year I've made more of an effort to read. I track books in my [/library](/library) page.

Although I have a [Nova Air 2 E-Note device](https://shop.boox.com/products/novaair2), given the choice, I prefer physical books. Despite the conveniene of an electronic note-taking device, theres something about analog that I find hard to quit. 

As I read books, especially non-fiction, I annotate them in various ways. Eventually, those annotations make their way into my library page.

Here's an example of those annotations for [Building A Second Brain by Thiago Forte](/reviews/building-a-second-brain).

The process of transferring notes is manual and tedious. I don't always have the discipline to transfer them at periodic intervals and what ends up happening is, I get to the end of the book without transferring any notes. To make space for new books, I donate it or resell the ones I've read. If I didn't take the time to transfer those notes, they're gone. [Slow Productivity](/reviews/slow-productivity-newport) is an example of that.

I want to find a better system that's low-maintenance for keeping more of these notes and retaining knowledge I've found valuable. 

Then it hit me, why not use AI? I know I could use OCR or even some of the AI models of yesteryear. The challenge is, those systems are error prone and given I don't always have the motivation to transfer notes manually, I have even less motivation to build and maintain such a system.

However, vision models have advanced significantly and when paired with language models, the barrier to entry for reasoning over image data has drastically decreased. 

That's what led to this post. In this post, I'll show how you can use AI models like GPT-4o Mini to extract the passages I've annotated in physical books from an image. I then format those passages in markdown to make them easy to directly copy and paste them onto the website. 

I know there's probably a ton of services that do this for you, but it's a lot more fun to build one from scratch. With that in mind, let's get started. 

You can find the source for the application in the [AIBookmarks GitHub repository](/github/AIBookmarks). 

Alternatively, I've configured the repo to use GitHub CodeSpaces, so you can launch the application there as well.

[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/lqdev/AIBookmarks)

## Configure access to GitHub Models

I'll be using [GitHub Models](https://github.blog/news-insights/product-news/introducing-github-models/) as my AI model provider. GitHub Models provides developers with access to a catalog of AI models such as GPT-4o, Llama 3.2, Phi 3.5, and many others. Best of all, it's completely free, [though restrictions apply](https://docs.github.com/en/github-models/prototyping-with-ai-models#rate-limits).

I chose GitHub Models for the following reasons:

- Zero installation. Although I really like Ollama and some of the other local AI model providers, I didn't want to fill precious hard-drive space with AI models.
- It's free! Since I'm just prototyping, even with their limited capacity, it should be enough to prove out whether my scenario is feasible.
- They provide access to multi-modal models such as GPT-4o, Llama 3.2 Vision, and Phi 3.5 Vision, which can reason over text and images, which is what I need.
- I'm one of the GPU poor. My [Lenovo Thinkpad X1](https://www.lenovo.com/us/en/p/laptops/thinkpad/thinkpadx1/x1-titanium-g1/22tp2x1x1t1?orgRef=https%253A%252F%252Fduckduckgo.com%252F) couldn't handle running one of the vision models.

Getting set up with GitHub models is fairly easy. At minimum, it requires:

1. A GitHub Account.
1. A Personal Access Token. For more details, see the [GitHub documentation](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens).

Once you have both of those, you can get started building your application.  

## Create a client

The sample application is a C# console application which targets .NET 9. However, the code shown here should work on the latest LTS version as well.

For this solution, I use the Azure AI Inference implementation of [Microsoft.Extensions.AI](https://www.nuget.org/packages/Microsoft.Extensions.AI.AzureAIInference/). [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/), M.E.AI for short, is a set of abstractions that provides a common set of interfaces for interacting with AI models.

Although we're using the Azure AI Inference client SDK implementation of M.E.AI, we can still use it to work with models from the GitHub Model catalog. 

```csharp
IChatClient client =
    new ChatCompletionsClient(
        endpoint: new Uri("https://models.inference.ai.azure.com"), 
        new AzureKeyCredential(Environment.GetEnvironmentVariable("GITHUB_TOKEN")))
        .AsChatClient("gpt-4o-mini");
```

## Load your images

In the repo, I have a set of sample images containing annotations. These are the images I'll send to the AI model for processing. 

The following is an sample from [The Creative Act by Rick Rubin](/reviews/creative-act-way-of-being-rubin).

![An image of a book with pencil markings](https://github.com/lqdev/AIBookmarks/raw/main/data/creative-act-1.jpg)

1. Load the files

    ```csharp
    var filePaths = Directory.GetFiles("data");
    ```

1. Create a collection to store extracted passages

    ```csharp
    var passages = new List<AIBookmark>();
    ```

## Process the images

Once you have the images loaded, it's time to proces them.

1. Start by setting the system prompt. This will provide the initial guidance for the extraction task.

    ```csharp
    var systemPrompt = 
        """
        You are an AI assistant that extracts underlined, highlighted, and marked passages from book page images.

        When passages have a natural continuation between pages, merge them and assign the page number where the first passage starts.
        """;
    ```

1. Then, iterate over each of the images and process them.

    ```csharp
    foreach(var path in filePaths)
    {
        var file = await File.ReadAllBytesAsync(path);
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt),
            new ChatMessage(ChatRole.User, new AIContent[] {
                new ImageContent(file, "image/jpeg"),
                new TextContent("Extract the marked passages from the image"),
            })
        };

        var response = await client.CompleteAsync<List<AIBookmark>>(messages, options: new ChatOptions {Temperature = 0.1f});

        passages.AddRange(response.Result);
    }
    ```

    This code:

    1. Loads the image as a `byte[]`.
    1. Composes a set of messages that include:

        - The system prompt
        - The image
        - The user prompt instructing the model to extract the marked passages from the image

    1. Sends the messages to the model for processing and returns a list of `AIBookmark`. An `AIBookmark` is a custom class I've defined as follows:

        ```csharp
        class AIBookmark
        {
            public string Text {get;set;}

            public int PageNumber {get;set;}
        }
        ```

        Some of the more recent AI models support structured output, which enforce a schema on AI model outputs. You can take a look at the [OpenAI documentation](https://openai.com/index/introducing-structured-outputs-in-the-api/) for more details. It's important to note though that the functionality is not exclusive to OpenAI models.

    1. Adds the extracted passages to the `passages` collection.

## Format the results

Once all of the files are processed by the AI model, additional processing is done to ensure that they're in the correct page order as well as formatted as markdown blockquotes. 

```csharp
var sortedPassages = 
    passages
        .OrderBy(p => p.PageNumber)
        .Select(p => $"> {p.Text} (pg. {p.PageNumber})");
```

## Display the results

```csharp
foreach(var passage in sortedPassages)
{
    Console.WriteLine(passage);
    Console.WriteLine("");
}
```

For the images included in the repo's *data* directory, output might look like the following.

```markdown
> This isn’t a matter of blind belief in yourself. It’s a matter of experimental faith. (pg. 278)

> When we don’t yet know where we’re going, we don’t wait. We move forward in the dark. If nothing we attempt yields progress, we rely on belief and will. We may take several steps backward in the sequence to move ahead. (pg. 278)

> If we try ten experiments and none of them work; we have a choice. We can take it personally, and think of ourselves as a failure and question our ability to solve the problem. (pg. 278)

> Staying in it means a commitment to remain open to what’s around you. Paying attention and listening. Looking for connections and relationships in the outside world. Searching for beauty. Seeking stories. Noticing what you find interesting, what makes you lean forward. And knowing all of this is available to use next time you sit down to work, where the raw data gets put into form. (pg. 296)

> Just as a surfer can’t control the waves, artists are at the mercy of the creative rhythms of nature. This is why it’s of such great importance to remain aware and present at all times. Watching and waiting. (pg. 296)

> Maybe the best idea is the one you’re going to come up with this evening. (pg. 297)
```

## Improvements and next steps

Putting the application together took me less than an hour so this is far from done. However, it does provide me with a starting point and offers validation that this could be a way to more easily capture the knowledge I'm curating from physical books. 

Some improvements I can make here:

1. Update the system prompt with some samples to help guide the extraction. For example, in the output I shared, that last passage is not annotated. It just happens to be a mostly blank page with that quote in the center, therefore giving the illusion that the passage is important in some way.  
1. Add additional information to the AIBookmark class like `Index` so I can ensure order within a page is preserved. Right now page number is good enough, but I can't guarantee the correct order. An index property might help here.
1. Use a service with higher rate limits. The current rate limits wouldn't allow me to process a large number of images at once. Therefore, I'd need to use a service with higher limits. Alternatively, I could make this a job that runs in the background which abides by the rate limits but I also don't have to spend money on. Given I'm not using this for anything mission-critical, that'd be an acceptable solution as well. 
1. Refactor the solution so I can more easily swap between tasks. For example, sometimes I might want to use it with images from my bullet journal. Other times, I might want to use it with handwritten notes. Whatever the case may be, it'd be good to not have to rewrite the prompts every time. 

Some ways I see myself using this project:

1. Periodically collect images of annotated pages and save them to cloud storage. 
1. When I'm done with the book, drop all the images in the *data* directory.
1. Further enrich data by condensing repetitive passages and extracting key concepts.
1. Storing this knowledge into some sort of knowledge store to make it actionable. 

## Conclusion

Just for fun, Tyler released all of the lyrics to his [latest album](/responses/chromakopia-tyler-the-creator-released) as [images on X](https://twitter.com/tylerthecreator/status/1852105825650708651/photo/1). With a few tweaks, I was able to repurpose this solution to extract the text from them and that worked relatively well. 

Just with this simple solution, there's a ton of other applications I can think of in my daily life to help bridge my analog and digital lives. 

What other use cases do you see yourself using something like this for? [Let me know](/contact).

Happy coding!