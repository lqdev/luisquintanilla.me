---
post_type: "article" 
title: "Site redirects in static websites"
description: "Learn how to create redirects in your static website without a server and JavaScript"
published_date: "07/31/2022 14:52"
tags: http, web, staticweb, indieweb, html
---

## Introduction

I recently came across the concept of [owning your links](https://indieweb.org/own_your_links) on the IndieWeb wiki and right away it made lots of sense to me. The main idea behind owning your links is using your domain as the source of truth and using it to redirect to your content on various other platforms you own. For example, when sharing my Twitter profile, instead of saying, "Go to https://twitter.com/ljquintanilla", I can instead point people to http://lqdev.me/twitter". The link on my domain ultimately redirects to Twitter, but the main entrypoint is my website. There's various ways to handle redirects, but in most cases it's done on the server side. For static websites, if you're hosting them in GitHub Pages, Netlify, or Azure Blob Storage, you don't have a server. Therefore configuring redirects can be a challenge. In this post, I'll go through how you can configure redirects for your static website without a server or JavaScript. 

## Create redirect page

Depending on which generator you use to create your static website, how you create your  redirect pages will differ. However, the end result should look like the the following:

```html
<html>
    <head>
        <meta http-equiv="refresh" content="0;REDIRECT-TARGET-URL">
    </head>
</html>
```

The only thing you need to perform the redirect is add a `meta` tag to the endpoint you want to handle the redirect. If I wanted to redirect to Twitter, I might create an HTML page at `http://lqdev.me/twitter` on my website. Setting the `http-equiv` attribute to `refresh` instructs the browser to refresh the page. Then, you use the `content` attribute to define your redirect target and how long you want to wait before redirecting the user. In the snippet above, the number `0` indicates you want to wait 0 seconds before redirecting your users and the `REDIRECT-TARGET-URL` is where you want to redirect to. This could be your Twitter profile, YouTube channel, or anywhere else. Wherever you want to redirect to, you place it in the `content` attribute. 

It's important to note that the redirect happens automatically which takes control away from the user causing potential accesibility issues. Until now, I haven't found a better way of doing this that doesn't require JavaScript. For more information, see the [web content accessibility guidelines](https://www.w3.org/WAI/standards-guidelines/wcag/).

For more information on creating redirects in HTML, see the article [how to redirect a web page in HTML](https://www.w3docs.com/snippets/html/how-to-redirect-a-web-page-in-html.html).

## Conclusion

So far I've created redirect pages for most of my social media profiles. You can see samples in my [contact page](/contact.html). It's important to note that while this works for social profiles and any other properties you own on the internet, it could also be used in cases where you've changed the structure of your website and want to gracefully handle redirects for broken / updated links. Happy coding!