---
post_type: "article" 
title: Alternatives to WhatsApp
published_date: 2021-01-09 13:44:27
tags: [privacy, self-hosted, security, open-source, apps]
---

## Introduction

Recently I've heard a lot of buzz around people moving away from the WhatsApp messaging app. I'm not tuned into all the details as to why there's been a mass exodus, but from what I understand it has to do with its privacy policies. I've seen many people including Elon Musk recommend Signal. Signal is a great alternative but there are a few others that are worth considering. This recommendation was originally planned as a tweet, but there's so many details that just can't be captured in a tweet or thread so instead I turned it into a blog post. If you haven't made the switch or if you're still not completely committed to the platform you've switched to, this post is for you.

![Phone and notebook - Oscar Mucyo Unsplash](https://user-images.githubusercontent.com/11130940/104107289-08689180-5289-11eb-890d-54527902df97.png)

## Matrix

![Element web client screenshot](https://user-images.githubusercontent.com/11130940/104107320-3e0d7a80-5289-11eb-9449-71e34315b96e.png)

[Matrix](https://matrix.org/) is my go-to messaging platform. Matrix is an open-source project that provides an open standard for real-time communication. Built on top of this standard are several implementations with [Synapse](https://github.com/matrix-org/synapse) being the most popular server and [Element](https://element.io/) being the most popular client.

Matrix is relatively new. However, it has a lot of potential and there are a lot of exciting things happening around it. Some of the most notable include:

- [Mozilla selected Matrix](https://matrix.org/blog/2019/12/19/welcoming-mozilla-to-matrix) to be the successor to its IRC communications.
- Matrix recently [acquired Gitter](https://matrix.org/blog/2020/09/30/welcoming-gitter-to-matrix), a popular chat platform for software projects, and has quickly worked to [integrate Gitter with Matrix](https://matrix.org/blog/2020/12/07/gitter-now-speaks-matrix) allowing for it to take advantage of many of the features provided by the protocol. 
- [Matrix will power FOSDEM 2021](https://matrix.org/blog/2021/01/04/taking-fosdem-online-via-matrix) communications.
- [Matrix is experimenting with P2P architectures](https://matrix.org/blog/2020/06/02/introducing-p-2-p-matrix) to help users have full control over their communications.

Although Matrix provides several features commonly associated with messaging platforms such as 1:1 messaging, video/audio calls, and group chat, there are a few features worth highlighting:

- End-to-end encryption
- Interoperability with other communication platforms
- Decentralization

### End-to-end encryption

End-to-end encryption makes sure your communications stay private. Only you and the intended recipients of the message are able to see the messages. 

### Interoperability

One of the biggest challenges of moving over to a new platform is that your friends and family don't or won't join you, making it a lonely place. Matrix provides interoperability with other communication platforms and protocols through [bridges](https://matrix.org/bridges/). Bridges facilitate the exchange of messages with other platforms without requiring all participants to be on Matrix. The core team maintains bridges for Slack, IRC, XMPP, and Gitter. There are also community maintained bridges for WhatsApp, Discord, Facebook Messenger, Signal, and many others. 

### Decentralization 

Decentralization means that there's no central authority that holds control over the platform or data. Matrix is decentralized through federation. One of the most common examples of federation is e-mail. I may have an AOL e-mail address and you may have a Hotmail e-mail address, yet we're still able to exchange messages. With Matrix, in a similar way, users and groups don't have to have their accounts on the same server in order to communicate. It also means that users have a choice over [which server they join](https://www.hello-matrix.net/public_servers.php). If I don't agree with the conversations or moderation of a particular server, I have the choice to open an account on another server without leaving Matrix altogether. Even better, because Matrix is built using open technologies, I have the option of hosting my own server.

To get started with Matrix, visit the [Element](https://element.io/get-started) website and create an account.

## Jami

![Jami Screenshot](https://user-images.githubusercontent.com/11130940/104107348-64331a80-5289-11eb-8606-944269e17d10.png)

Another project that I like but haven't used it to the same extent as Matrix is [Jami](https://jami.net/). Jami is an open-source project for real-time communication. Similar to Matrix, Jami is cross-platform, free (as in freedom and beer), end-to-end encrypted, and decentralized. Jami achieves decentralization by using P2P protocols. This means that the data is stored on your device so you're in full control of your data. Jami also doesn't require the internet to work. As long as you're in the same local network as your peers, you're able to communicate. 

To get started with Jami, [download the app](https://jami.net/download/) and create an account.

## Conclusion

In this post, I've mentioned some communication platforms that I consider great alternatives to WhatsApp. Regardless of the platform you choose, some things that I believe should be top of mind are security and privacy. It's important that you own your data and you have control over who has access to it and what they do with it. Feel free say hi on Matrix [@lqdev:lqdev.tech](https://matrix.to/#/@lqdev:lqdev.tech) or Jami where you can find me as lqdev1.