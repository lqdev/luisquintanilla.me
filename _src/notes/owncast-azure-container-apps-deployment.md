---
post_type: "note" 
title: "Got an Owncast instance deployed to Azure!"
published_date: "2025-01-07 19:52 -05:00"
tags: ["owncast","livestream","azure","selfhost","fediverse","obs","containers","aca"]
---

A few months ago, I started tinkering with Owncast. In that case, I was figuring out how I could use .NET Aspire and Blazor to [configure an Owncast server and embed the stream on a website](/posts/build-your-own-live-streaming-app-owncast-dotnet-aspire/).

Although I didn't go all the way through creating the deployment, doing so with [Aspire would've been fairly straightforward](https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment).

Since I already have a website, all I care about deploying is the Owncast server. 

After spending about an hour on it today, I was able deploy and Owncast server to Azure Container Apps using the [virtual network environment setup tutorial](https://learn.microsoft.com/en-us/azure/container-apps/vnet-custom?tabs=bash&pivots=azure-portal).

The hardest part was figuring out that I needed to set up a virtual network in order to publicly expose multiple ports. 

Once the server was up and running, configuring OBS was relatively easy as well.

![AI Generated image of two aliens enjoying a beer in the middle of the desert by a campfire on a starry night](http://cdn.lqdev.tech/files/images/owncast-obs-azure-container-apps-deployment.png)

I'll try to put together a more detailed writeup so I remember what I did for next time as well as help others who may be interested in self-hosting their own livestream server. 