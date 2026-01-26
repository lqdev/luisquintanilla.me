---
post_type: "article" 
title: "Webmentions are back thanks to GitHub Copilot"
description: "An update on how I upgraded and got my webmentions endpoint back online with the help of GitHub Copilot"
published_date: "2026-01-25 19:06 -05:00"
tags: ["indieweb","webmentions","protocol","azure","ai","github","copilot","claude","mcp","perplexity","anthropic"]
---

Webmentions are working again on this site. 

Earlier today I deployed a new version of my webmention service to Azure and brought my endpoint back online.

A few months ago, I moved my website from [Azure Storage static website hosting](https://learn.microsoft.com/azure/storage/blobs/storage-blob-static-website) to [Azure Static Web Apps](https://learn.microsoft.com/en-us/azure/static-web-apps/overview) for cost reasons. As part of that migration, I also removed Azure Front Door which mapped my custom domain and provided certificates for my webmention endpoint. As expected, this broke my webmention endpoint and since it wasn't high on my priority list, it just stayed broken for months. 

Yesterday I decided to bring the webmentions endpoint back online. In part because I want to receive webmentions again, but also because [my website now implements parts of the ActivityPub protocol](/notes/website-now-natively-posts-to-the-fediverse-2026-01-22/). That means that my website is effectively a node in the Fediverse. At the moment it doesn't handle activities other than Follow requests and delivers my posts to my followers. Ultimately, I'd like to do something similar to [BridgyFed](https://fed.brid.gy/). Basically take any interactions on my Fediverse posts (Boost, Like, Bookmark, Reply) and map them to webmentions so I'm notified when someone engages with content on the various Fediverse platforms like Mastodon. So in order to do that, I needed my webmentions endpoint back online. 

In this post, I'll give a brief overview of my solution, what's new, and talk about how I got my endpoint working again with the help of AI.

## Solution overview

My webmention endpoint is hosted on Azure. 

More specifically, it's an Azure Functions project made up of two functions:

- **Receive Webmentions** - When someone sends a webmention to the endpoint, it uses the [WebmentionFs](https://www.nuget.org/packages/lqdev.WebmentionFs) library to perform validation per the [Webmentions W3C spec](https://www.w3.org/TR/webmention/) and stores the webmention in Azure Table Storage. The endpoint accepts like, repost, reply, and bookmark webmentions.
- **Webmention to RSS** - Generates an RSS file every day at 3 AM UTC with the latest webmentions stored in Azure Table Storage. This one's not required per the Webmention spec, but it's the solution I came up with for consuming and getting notifications about the webmentions I receive.

You can find the solution in the [lqdev/WebmentionService repo on GitHub](https://github.com/lqdev/WebmentionService).

For more details about the solution, you can read my post [Accepting Webmentions using F#, Azure Functions, and RSS](/posts/receive-webmentions-fsharp-az-functions-fsadvent/).

Although there's been some changes which I'll talk more about below, the solution has remained largely unchanged. 

## What's new

It had been some time since I last updated the solution so I took some time to upgrade it. Here are some of the highlights:

- **Upgrade to .NET 10** - Since I originally deployed my endpoint back in 2022, it remained largely untouched. In fact, I don't think I ever updated or redeployed it. Even if I did, it happened at most once or twice in the past 3 years. I know my service kept working though because I had webmentions through August / September 2025 which I find amazing. The original solution was running .NET 6 which is no longer supported. My solution is running .NET 10 now. 
- **Standard dependency injection** - I don't remember if I was running the [isolated worker model](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide?tabs=ihostapplicationbuilder%2Ccode%2Cwindows#benefits-of-the-isolated-worker-model) or in-process for my original solution. Even if I was running isolated worker, I wasn't using standard dependency injection which is a new change. Now my solution looks like any other ASP.NET Web API which is nice. 
- **Flex Consumption Plan** - My original solution was using the [Consumption Plan](https://learn.microsoft.com/azure/azure-functions/consumption-plan). Azure Functions plans [to retire that in 2028](https://azure.microsoft.com/en-us/updates/?id=499451), so I was proactive about it and just migrated to the [Flex Consumption Plan](https://learn.microsoft.com/azure/azure-functions/flex-consumption-plan) as recommended. Since my service is low-traffic, I should be able to use the [Free Tier](https://azure.microsoft.com/pricing/details/functions/#pricing) for a total cost of $0. Also worth mentioning, a more immediate reason for migrating to the Flex Consumption Plan is that .NET 10 on Linux is only supported on that plan. So by upgrading to .NET 10, I was also forced to switch to Flex Consumption Plan.  
- **Custom domain and Azure certificate** - While not exactly new because I was using custom domains before, I'm no longer using Azure Front Door. Azure Front Door was good but way too much for what I needed both in terms of features and cost. Now I'm just pointing my DNS configuration to my Azure Functions endpoint and I'm using a certificate from Azure. 
- **GitHub Actions CI/CD Deployment Workflow** - Deployment was manual. I took the opportunity to add a new GitHub Actions workflow to deploy the latest versions of my solution to Azure whenever there's a merge into the main branch. 
- **Documentation** - I made the mistake the first time of not documenting my process and solution, other than the blog post. As a result, I wasn't sure where to get started to maintain it. That's why I rarely touched it in 3+ years. This time, I made sure to add extensive documentation and a workflow for project management, architectural decision records, changelogs, etc. 
- **AGENTS.md / Copilot Instructions** - As I'll mention in the next section, this upgrade / migration was entirely done using AI. To help guide the AI coding assistants, I added AGENTS.md and Copilot Instructions. 

For a full list of changes, check out the [changelog](https://github.com/lqdev/WebmentionService/blob/main/docs/changelog.md).

## AI Coding Workflow

To perform the migration, I wrote none of the code, provisioned any of the Azure resources, or performed the deployments. Everything was done by AI. I used:

- [GitHub Copilot](https://code.visualstudio.com/docs/copilot/overview)
    - Claude Opus 4.5
    - Claude Sonnet 4.5
- [Model Context Protocol (MCP)](https://modelcontextprotocol.io/docs/getting-started/intro) servers
    - [Microsoft Learn](https://learn.microsoft.com/training/support/mcp-get-started)
    - [Perplexity](https://docs.perplexity.ai/guides/mcp-server)
- [GitHub CLI](https://docs.github.com/en/github-cli/github-cli/quickstart)
- [Azure CLI](https://learn.microsoft.com/cli/azure/get-started-with-azure-cli?view=azure-cli-latest)

### Planning

My original goal for this project was simple.

1. Upgrade to .NET 10
1. Deploy the upgraded version of my app to Azure

Those instructions plus additional instructions to understand the solution paired with context like the blog post detailing the solution were effectively the prompt I provided GitHub Copilot. 

For the planning phase, I used Claude Opus 4.5 as the model and also gave it access to the Azure CLI and the Microsoft Learn and Perplexity MCP servers. 

With that, GitHub Copilot set off to:

1. Use the blog post to understand motivations and original technical design decisions.
1. Inspect the code to understand the structure of the project.
1. Execute Azure CLI commands to get Azure resource and deployment details.
1. Look up information using Perplexity and Microsoft Learn MCP servers about what needed to be done to upgrade my project to .NET 10 and how to do it. 

The result was two artifacts:

- [Flex Consumption Migration Project Plan](https://github.com/lqdev/WebmentionService/blob/main/docs/projects/completed/flex-consumption-migration.md)
- [Flex Consumption .NET 10 Migration Architectural Decision Record (ADR)](https://github.com/lqdev/WebmentionService/blob/main/docs/adr/0002-migrate-to-flex-consumption-for-dotnet10.md)

These documents served as the guide for the rest of the migration. As deviations or unexpected roadblocks came up, these documents were updated to reflect those changes. 

### Inner Loop

Once I had the ADR and project plans in place, it was time to start the migration. 

This was entirely done by GitHub Copilot. Since the plans were effectively implementation ready, I switched from Claude Opus 4.5 to Claude Sonnet 4.5. This kept my cost down while still performing effectively on the coding tasks. Throughout the implementation, if it needed to look up information, it used the Perplexity and Microsoft Learn MCP servers. 

As mentioned earlier, if there were roadblocks or deviations from the original plan, once it validated and arrived at a solution, I asked it to update the plan and sometimes the ADR to reflect these changes. 

### Outer Loop

Once code was complete, version control, resource provisioning, deployment, and monitoring were done primarily by having GitHub Copilot execute GitHub CLI and Azure CLI commands. Again, if it needed additional information, it used Perplexity or Microsoft Learn as references. 

As expected, there was some iteration after deployment. Using the GitHub CLI I was able to diagnose and debug GitHub Actions issues and using the Azure CLI, I was able to diagnose issues in the deployed solution. 

Once issues were diagnosed, it was back to either the planning or coding phase until we got to the solution working successfully.

## Conclusion

Overall, I'm happy to not only have my webmentions endpoint back online but it's back better than ever with all the upgrades. More importantly, it establishes a foundation for me to continue to build upon as I build integrations with my ActivityPub implementation. I know at some point I'll want to add [AT Protocol](https://atproto.com/) and [Nostr](https://nostr.com/). Nostr particularly feels like a relatively easy addition now that I've established a pattern with my ActivityPub implementation so [I just know I'll end up doing it at some point](/posts/nostr-first-impressions/).

If you're looking to build your own webmentions endpoint, hopefully this can serve as a reference implementation. If you find it useful, send me a webmention and let me know!