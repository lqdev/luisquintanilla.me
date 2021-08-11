---
title: Self-hosting made easy with YunoHost
description: YunoHost can help you self-host applications and services by simplifying your deployment process. 
date: 2021-08-10 18:00:00
tags: fediverse, open-source, self-hosting, raspberry-pi, matrix, pixelfed, mastodon
---

## Introduction

Over the past few months I've started self-hosting a few of the services I use. Among those are a Matrix chat server and Mastodon instance. It may be of special interest to call out, many of these services are hosted on a Raspberry Pi 4, which not only speaks to the capabilities of the device but also shows opportunities for individuals to take control over the services they use and their data. Self-hosting however isn't the easiest thing to get started with. Mainly because each service has their own setup requirements. Technically you could use something like Docker to standardize the process, but that has its own complexities. In particular, there was a recent experience where I had trouble setting up a service which I got up and running in no time using YunoHost. YunoHost describes itself as "...a libre operating system aiming to simplify server administration and democratize self-hosting.". In this post, I'll describe my experience with it and why you might consider it for your self-hosting project.

![Image of server racks by Taylor Vick (Unsplash @tvick)](https://user-images.githubusercontent.com/11130940/128647305-c30ef44e-901d-4bf1-8e9d-af2449d5fd1d.png)

## Why Self-Host?

First off, why even go through the trouble of self-hosting? Service providers nowadays have made it so easy to use their services, self-hosting seems like a lot of work. However, there are several reasons why you might consider self-hosting the applications and services you use. A few that I find interesting include:

1. Explore "new" technologies
2. Own your data
3. Learn something new

### Explore "new" technologies

The saying goes "there's nothing new under the sun". However as with all things, people have found alternative uses and solutions to existing technologies. For example, in simple terms, the [Fediverse](https://en.wikipedia.org/wiki/Fediverse) can be summarized as E-mail + RSS. It consists of a common decentralized protocol to handle communication between individuals who may not use the same server while providing "follow" capabilities for services like micro-blogging, photos, and video sharing. Though the Fediverse has been around for some time, it's still relatively new. Whether it's a new technology or new to you, self-hosting can help you learn more about technologies like the Fediverse.

### Own your data

Part of accepting terms of service agreements is sometimes giving up some ownership of your personal data. Although you hope the company and service provider is responsible with your data, you can't always be 100% sure. With self-hosting, you're fully in control of your data and in most cases don't rely on third parties. As a result, your data is as private and secure as you want it to. From a security standpoint though, I'll note that in many cases, service providers have dedicated security teams whose main job is to keep your data secure. Therefore chances are that your data is usually secure when hosted by service providers. In any case, if you want to have complete control and ownership of your data, self-hosting might be for you.

### Learn something new

Over the past few months, I've learned a few things that I don't usually deal with on my day-to-day like DNS records, Nginx, Systemd, certificates, and a few others. Self-hosting these services usually involves setting up several components some of which you may already be familiar with while others may be new to you. Self-hosting gives you the opportunity to learn new skills and as mentioned previously work with new technologies.

## Self-hosting with YunoHost

Recently I was trying to set up an instance of [Pixelfed](https://pixelfed.org/), which describes itself as "A free and ethical photo sharing platform". You can think of it as a self-hosted Instagram. The application is built with PHP and a few other components I'm not very familiar with. I spent about a week trying to configure my Raspberry Pi to successfully run these services. Unfortunately, I had no luck. I had heard about YunoHost previously but had never tried it. Essentially they offer a way to simplify self-hosting. I noticed YunoHost provided a way to self-host Pixelfed. All I had to do was:

1. [Install YunoHost](https://yunohost.org/en/install). In my case, I installed it on a Linode VM (the smallest one) running Debian 10, but you have options including a Raspberry Pi.
2. [Install Pixelfed](https://github.com/YunoHost-Apps/pixelfed_ynh). YunoHost has a variety of [applications](https://yunohost.org/en/apps) with pre-configured scripts that make it easy to install applications like Pixelfed.

The entire process took about one hour. Some parts I skipped mentioning were getting a domain name and configuring DNS records, but aside from that, getting started with YunoHost was incredibly simple. If you're looking to self-host but don't want to spend too much time configuring your server and each of the services you host on it, give YunoHost a try.
