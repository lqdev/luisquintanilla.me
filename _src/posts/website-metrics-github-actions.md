---
post_type: "article" 
title: "Computing website metrics using GitHub Actions"
description: "A post detailing how I use GitHub Actions to automate computing aggregate annual metrics for various post types on my website"
published_date: "2023-12-24 13:24"
tags: ["github","cicd","website","blog","blogging","web","internet","fsharp","dotnet"]
---

## Introduction 

I recently posted my [(We)blogging Rewind](/feed/weblogging-rewind-2023) and [(We)blogging Rewind Continued](/feed/weblogging-rewind-2023-continued) for 2023. In these posts, I discuss some analysis I did of my posting behaviors over the past year on this website. I don't use platforms like Google Analytics to track visitors. I used to self-host a [GoatCounter](https://www.goatcounter.com/) instance but personally I don't really care about traffic so I got rid of that. There are some standard reports I get from my [CDN provider on Azure](https://learn.microsoft.com/azure/cdn/cdn-advanced-http-reports), but again I don't really care about those metrics. What I do care about though is my output and understanding what I'm publishing and the topics that were important to me at any given time. In the case of those blog posts, it was for the year 2023. Given that I had already done the analysis and had written the script, I thought, why not automate it and run it on a more regular basis to have monthly summaries. Since my blog and scripts are already on GitHub, it makes sense to create a GitHub Action workflow. In this post, I discuss in more details what my post analytics script does and how I configured my workflow in GitHub Actions to run the script on the first of every month.  

## The script

This script loads the various posts on my website and computes aggregate metrics based on post types and their metadata. You can find the full script at [stats.fsx](/resources/snippets/lqdev-me-website-post-metrics).

### Loading files

The following are convenience functions which I use as part of my website build process. 

In general these functions:

- Load the individual post files
- Parse the content and YAML metadata

```fsharp
let posts = loadPosts()
let notes = loadFeed ()
let responses = loadReponses ()
```

Instead of building new custom functions, I can repurpose them and apply additional transformations to compute aggregate statistics.

### Computing aggregate statistics

Once the posts are loaded, I apply transformations on the collections to compute aggregate metrics:

#### Annual post counts

The following are annual aggreagates of blog posts, notes, and responses. 

##### Blog posts

Takes the blog post collection, parses the published date, and computes counts by the year property. Then, it sorts them in descending order.

```fsharp
let postCountsByYear = 
    posts
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst 
```

##### Notes

Takes the note collection, parses the published date, and computes counts by the year property. Then, it sorts them in descending order.

```fsharp
let noteCountsByYear = 
    notes
    |> Array.countBy (fun (x:Post) -> DateTime.Parse(x.Metadata.Date) |> _.Year)
    |> Array.sortByDescending fst
```

##### Responses

Takes the response collection, parses the published date, and computes counts by the year property. Then, it sorts them in descending order.

```fsharp
let responseCountsByYear = 
    responses
    |> Array.countBy (fun (x:Response) -> DateTime.Parse(x.Metadata.DatePublished) |> _.Year)
    |> Array.sortByDescending fst
```

#### Response counts by type

Takes the response collection, parses the published date, filters it for the current year, and computes counts by the post type (reply, bookmark, reshare, star).

```fsharp
let responsesByType = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> x.Metadata.ResponseType)
    |> Array.sortByDescending(snd)
```

#### Tag counts (responses)

Takes the response collection, parses the published date, filters it for the current year, and computes counts by the tag name, and sorts in descending order using the count.


```fsharp
let responsesByTag = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.collect(fun x -> 
            match x.Metadata.Tags with
            | null -> [|"untagged"|]
            | [||] -> [|"untagged"|]
            | _ -> x.Metadata.Tags
        )
    |> Array.countBy(fun x -> x)
    |> Array.sortByDescending(snd)
```

#### Domain counts (responses)
 
Takes the response collection, parses the published date, filters it for the current year, and computes counts by the target URL host name, and sorts it in descending order using the count.

```fsharp
let responsesByDomain = 
    responses
    |> Array.filter(fun x -> (DateTime.Parse(x.Metadata.DatePublished) |> _.Year) = DateTime.UtcNow.Year)
    |> Array.countBy(fun x -> Uri(x.Metadata.TargetUrl).Host)
    |> Array.sortByDescending(snd)
```

### Displaying counts

Since the `countBy` function is the one used to compute the counts, this produces a tuple. The tuple though could be `string` or `int`. Therefore, I set the collection of entry counts to use a generic `'a` for the first item in the tuple. I'm also able to control using `n` whether to display the entire collection by using `-1` as input or a limit when `n >= 0`.

```fsharp
let printEntryCounts<'a> (title:string) (entryCounts:('a * int) array) (n:int) = 
    printfn $"{title}"

    match n with 
    | n when n = -1 -> 
        entryCounts
        |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
        |> fun _ -> printfn $""
    | n when n >= 0 -> 
        entryCounts
        |> Array.take n
        |> Array.iter(fun x -> printfn $"{fst x} {snd x}")
        |> fun _ -> printfn $""
```

The result of running this script produces the following results:

```text
Blogs
2023 5
2022 7

Notes
2023 34
2022 36

Responses
2023 216
2022 146

Response Types
bookmark 151
reshare 48
reply 10
star 7

Response Tags
ai 104
llm 42
untagged 41
opensource 31
internet 17

Domains
github.com 15
huggingface.co 11
arxiv.org 10
openai.com 6
www.theverge.com 4
```

## The workflow file

The workflow file is a GitHub Actions workflow which you can find in my [website repo](https://github.com/lqdev/luisquintanilla.me/blob/main/.github/workflows/stats.yml).  

### Triggers

I don't really want the script to run every time I publish my website. Instead, I just want to have these aggregate values computed on a monthly basis. Optionally though, I'd like to be able to run ad-hoc reports and trigger this job manually.

The triggers in my workflow file look like the following:

```yaml
schedule: 
  - cron: '30 0 1 * *'
workflow_dispatch: 
```

Using cron job syntax, I use the `schedule` trigger to configure the script to run at 12:30 AM on the 1st day of every month.

The `workflow_dispatch` trigger is there so I can manually trigger this job.

## Steps

The steps in the workflow file are the following:

- Check out the repo

    ```yaml
    - uses: actions/checkout@v2
    ```


- Install the .NET 8 SDK

    ```yaml
    - name: Setup .NET SDK 8.x
      uses: actions/setup-dotnet@v1.9.0
      with: 
        dotnet-version: '8.0.x'    
    ```

- Restore dependencies

    ```yaml
    - name: Install dependencies
      run: dotnet restore    
    ```

- Build the project

    ```yaml
    - name: Build project
      run: dotnet build --no-restore  
    ```

- Run the script and display metrics

    ```yaml
    - name: Display Post Metrics
      run: dotnet fsi Scripts/stats.fsx
    ```

## Conclusion

If you have scripts that you run on a repo on a fairly regular basis, consider using GitHub Actions to automate the execution of these scripts. Happy coding! 