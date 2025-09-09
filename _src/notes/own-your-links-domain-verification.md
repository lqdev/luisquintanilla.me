---
post_type: "note" 
title: "Use your domain for verification everywhere"
published_date: "2022-11-22 10:19 -05:00"
tags: ["indieweb","ownyourlinks","internet","domain","identity","technology","ownership"]
---

I've never had a blue checkmark on Twitter so I can't say what that process is like. Looking at it from the outside though it seems the process isn't simple or straightforward. My experience with verification has been with other applications such as [Mastodon](https://docs.joinmastodon.org/user/profile/#verification) and [IndieAuth](https://indieauth.com/). In both of these cases, the main verification techniques use your domain and/or the [`rel=me` link type](https://developer.mozilla.org/en-US/docs/Web/HTML/Link_types/me). Although with Mastodon you can add multiple verified links to your profile, verification on those external domains pose challenges which include but are not limited to:

- You can't edit the HTML directly so you can't add a `rel=me` link.
- You can only add one link, so you're forced to choose between your website and Mastodon.
- In scenarios where multiple links (i.e. e-mail, webpage, Twitter) are supported, Mastodon isn't one of the options

So how do you overcome these challenges? By using your domain. 

A few months ago I wrote about [owning your links](/posts/static-website-redirects). The general concept of owning your links is, using your domain to redirect to other properties and profiles you own. For example, instead of telling people to visit my Twitter profile at *twitter.com/ljquintanilla*, I can instead point them to *lqdev.me/twitter*. Not only does my domain become the source of truth, but in general it's easier to remember compared to usernames and proprietary domains. Since my site is statically generated, I own my links by using [http-equiv meta tags](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/meta#attr-http-equiv). Although the only job for these pages is to redirect to the specified site, in the end they are still HTML pages. This means, I can embed any other HTML I want, including `rel=me` links. By including the `rel=me` link in the redirect page, you satisfy the requirements of applications like Mastodon because that's the page they'll land on before being redirected to the target site. As a result, I can verify my GitHub, Twitter, or any other online property I want with Mastodon using my own domain without having to make any changes or overcome any of the challenges imposed by those other platforms. What you end up with is something like this:

![Screenshot of lqdev profile verified links](https://cdn.lqdev.tech/files/images/mastodon-profile-verified-links.png)

Before signing off, I'll note that this isn't a perfect solution and technically anyone could do the same thing on their own domain in an attempt to impersonate you, so just be aware of that.