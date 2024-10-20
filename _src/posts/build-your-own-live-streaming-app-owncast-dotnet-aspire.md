---
post_type: "article" 
title: "Build your own self-hosted live streaming application with Owncast and .NET Aspire"
description: "This post shows how you can combine the powerful live-steaming features of Owncast with the app orchestration and tooling in .NET Aspire to create rich self-hosted live-steaming applications."
published_date: "2024-10-20 14:01"
tags: ["owncast","dotnet","indieweb","fediverse","aspire","livestreaming","twitch","youtube","blazor"]
---

Platforms come and go. As a result, I'm a strong advocate for [owning your data](https://indieweb.org/own_your_data) (when possible). Self-hosting is a way for you to do that. Owning your data could mean self-hosting [your website](https://buttondown.com/ownyourweb/archive/issue-06/), [password manager](https://bitwarden.com/blog/host-your-own-open-source-password-manager/), [media server](https://jellyfin.org/docs/), or [social media](https://docs.joinmastodon.org/user/run-your-own/). However, self-hosting comes with its own challenges, primarily cost (time and money) and in cases where software isn't provided as an appliance on your hosting provider of choice, some technical knowledge may be required. 

As I got more into self-hosting applications such as my [Mastodon instance](/mastodon), I came across Owncast.   

During peak COVID lockdowns, like many others, live streaming is one of the ways I passed the time. While Twitch and YouTube got the job done, self-hosting was always in the back of my mind.  

It's been a while since I've live-streamed, so I never really went through the process of evaluating the self-hosted route with Owncast. 

While browsing for something on YouTube the other day, I ran into some of my [old live-stream recordings](https://www.youtube.com/playlist?list=PLsdMoYmuvh9ZtgB8U7FECR_8wKMYXJNAm). This got me thinking again, how difficult would it be to put together my own live-streaming setup. 

This post is the result of that exploration. 

In this post, I'll modify a .NET Aspire Starter Application template and show how to set up a self-hosted live-streaming application using Owncast and .NET Aspire.  

You can find the source code in the [lqdev/BYOwncastAspire](/github/BYOwncastAspire) repository.

![BYOwnCastAspire Web Frontend](https://github.com/user-attachments/assets/bdd9f901-8d8f-45be-9e37-4dce8459e481)

## What is Owncast?

The Owncast website describes the project as, "...a free and open source live video and web chat server for use with existing popular broadcasting software."

![Owncast admin server page](https://github.com/user-attachments/assets/769ec2d8-a3d1-4ec3-8271-c2de6e11ddd3)

It goes on to further describe some of the reasons I like Owncast, which are:

- **Self-hosting** - I'm in complete control over the service and my data
- **Open-source** - I can freely use and contribute to the project. Free in this case meaning both as in freedom and pizza.
- **Builds on top of open standards like RMTP** - Software that supports the Real-Time Messaging Protocol (RMTP) like OBS can immediately be leveraged. 
- **Fediverse compatible** - Your content and network federated across [the Fediverse](https://joinfediverse.wiki/What_is_the_Fediverse%3F). 

To learn more, check out the [Owncast website](https://owncast.online/). 

## What is .NET Aspire?

The .NET Aspire documentation describes it as, "...an opinionated, cloud ready stack for building observable, production ready, distributed applications.​ .NET Aspire is delivered through a collection of NuGet packages that handle specific cloud-native concerns."

Personally, the parts of .NET Aspire that matter to me are:

- **App composition** - The .NET Aspire programming model makes it easy to define and compose a variety of resources such as .NET projects, containers, and much more in a single place. In many cases, for commonly used services, these resources are exposed in the form of [integrations](https://learn.microsoft.com/dotnet/aspire/fundamentals/integrations-overview). These integrations further simplify the composition of applications.  
- **Tooling (Dashboard)** - .NET Aspire provides a set of tools and templates. However, my favorite is the dashboard. The dashboard provides me with a single place to view my resources, their configurations, and logs. 

Although in this post, I don't cover deployment, there is also the provisioning component provided by .NET Aspire which in many cases can [simplify your application deployments](https://learn.microsoft.com/dotnet/aspire/deployment/overview). 

To learn more, check out the [.NET Aspire documentation](https://learn.microsoft.com/dotnet/aspire/get-started/aspire-overview). 

## Build your application

This application makes a few modifications to the [.NET Aspire Starter Application template](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling?tabs=linux&pivots=dotnet-cli#net-aspire-project-templates). 

The application consists of a few projects:

- **BYOwncastAspire.AppHost** - This project is the entrypoint for .NET Aspire applications. This is where we'll configure the Owncast server as a container resource. 
- **BYOwncastAspire.Web** - Blazor web application. Although Owncast provides its own page where viewers can tune into your stream, by having a separate web application, preferably your own personal website, you can further enrich and customize how and where you publish content. 
- **BYOwncastAspire.ServiceDefaults** - This project contains default configurations for telemetry, health checks, resiliency, etc. No changes or updates required here. 

### Configure Owncast

There are many ways to host an Owncast server, with one of them being a container. 

In the context of Aspire which has built-in container support, you can easily add Owncast as a resource in your application. 

From the [Owncast documentation](https://owncast.online/quickstart/container/), once you've pulled the Owncast container image, you can start it with the following command.

```bash
docker run -v `pwd`/data:/app/data -p 8080:8080 -p 1935:1935 owncast/owncast:latest
```

This translates to the following in the *Program.cs* of the *BYOwncastAspire.AppHost* project.

```csharp
var owncast = builder
    .AddContainer(name: "owncast", image:"owncast/owncast")
    .WithBindMount("./data","/app/data")
    .WithHttpEndpoint(port:8080,targetPort:8080,name:"admin")
    .WithHttpEndpoint(port:1935,targetPort:1935,name:"streaming")
    .WithExternalHttpEndpoints();
```

This code:

- Adds the owncast container image
- Mounts a local *data* directory to the */app/data* directory in the container
- Maps port `8080` for the Owncast admin server and `1935` for RMTP server.
- Exposes the endpoints publicly

### Embed your stream

Owncast provides the ability to [embed your video stream onto a website](https://owncast.online/docs/embed/). 

Although we don't need a frontend because one is already provided by Owncast, by embedding your stream on your website you can provide a single place for your viewers to consume your content. 

In this case, we can treat the *BYOwncastAspire.Web* project as our website. 

To embed your stream to the website, add the following code to your *Home.razor* page.

```csharp
<iframe
  src="http://localhost:8080/embed/video"
  title="Owncast"
  height="350px" width="550px"
  referrerpolicy="origin"
  allowfullscreen>
</iframe>
```

In this case, we're pointing to the endpoint listening on port `8080` of our `localhost`. When you deploy the application, you'd replace `src` with your domain. 

## Start your application

That's all there is in terms of configuration. 

To start the application:

1. Open the terminal 
2. Navigate to the *BYOwncastAspire.AppHost* project and run the following command.

    ```bash
    dotnet run
    ```

This will launch you into the .NET Aspire dashboard. At this point, you can further customize your Owncast server as well as the website.  

![BYOwncastAspire .NET Aspire Dashboard Resource Page](https://github.com/user-attachments/assets/c20d84d8-925a-4f80-9058-622466cb08e9)

## What next?

Now that you have your application running, in its current form, this application is only meant to serve as a sample of what you can do with Owncast and .NET Aspire. 

Some next steps might include:

- [Change your admin password and customize your Owncast server](https://owncast.online/docs/configuration/)
- [Configure object storage](https://owncast.online/docs/storage/)
- [Set up your steaming software](https://owncast.online/docs/broadcasting/obs/)
- [Deploy your application](https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment)
- [Extend the app's functionality by building custom plugins and extensions](https://owncast.online/thirdparty/)

The plugins and extensions are particularly interesting to me because there may even be opportunities to experiment and insert AI capabilities at various layers such as moderation, translation, accessibility, show notes, chat summaries, etc. 

## Conclusion

In this post, I showed how you can combine the powerful live-steaming features of Owncast with the app orchestration and tooling in .NET Aspire to create rich self-hosted live-steaming applications.

If you use this sample as a starting point for your own live-streaming or self-hosting explorations, [send me a message](/contact). I'd love to hear about it.  
