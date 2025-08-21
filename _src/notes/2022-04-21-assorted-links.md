---
post_type: "note" 
title: "Assorted links - Ubuntu 22.04 LTS, Bitwarden, DALLE-2, Emacs"
published_date: "04/21/2022 19:44"
tags: ["links","assortedlinks","ubuntu","bitwarden","dalle","openai","emacs","linkblog"]
---

Here's a few links I found in the interwebs today that I thought were worth sharing.

## Ubuntu 22.04 LTS Released

For my personal computing needs, Manjaro has been my default Linux distro. However, for hosting and cloud VMs I go back and forth between Debian and Ubuntu depending on which better supports the software I'm running. Canonical announced the release of the latest Ubuntu LTS version 22.04. Check out their [blog post](https://ubuntu.com/blog/ubuntu-22-04-lts-released) for more details on what's new. 

## Self-hosting Bitwarden on DigitalOcean

When it comes to password managers, other than KeePass, my favorite it [Bitwarden](https://bitwarden.com/). It's open-source, built on .NET, has excellent features that get even better with the low-cost paid version, but most importantly provides you the option of self-hosting. If you're looking to self-host Bitwarden on DigitalOcean, make sure to check out the [guide](https://bitwarden.com/blog/digitalocean-marketplace/) they just published. 

## How DALLE-2 Actually Works

A few weeks ago, OpenAI announced the release of [DALLE-2](https://openai.com/dall-e-2/). Open AI describes DALLE-2 as, "...a new AI system that can create realistic images and art from a description in natural language." If you want to understand the details of how it works, you could read the 27 page research paper [Hierarchical Text-Conditional
Image Generation with CLIP Latents](https://arxiv.org/pdf/2204.06125.pdf). AssemblyAI just published an approachable guide into the inner workings of DALLE-2. If you're interested in that, check out their [blog post](https://www.assemblyai.com/blog/how-dall-e-2-actually-works/) on the topic.  

## Emacs Configuration Generator

In my early days of using Emacs, I often found it difficult to customize it to my needs. Not only did I not know what was possible, but it also caused some anxiety because I didn't feel confident enough making major tweaks. If you're getting started with Emacs and aren't sure how to configure it, this neat [utility](https://emacs.amodernist.com/) helps you generate a config file for Emacs.

## PostgresML 

If you're building machine learning applications, one of the main things you'll need is data. This data can reside in databases such as MySQL, SQL Server, and Postgres. Algorithms are applied to this data downstream using libraries written in languages like Python, R, and .NET. This not only means potentially moving the data from the database to another process but also using another library to train models. According to the project description on GitHub, "PostgresML is an end-to-end machine learning system. It enables you to train models and make online predictions using only SQL, without your data ever leaving your favorite database." If that's something you're interested in, check out the project on [GitHub](https://github.com/postgresml/postgresml). 