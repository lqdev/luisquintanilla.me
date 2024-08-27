---
post_type: "article" 
title: Blogging Tools
published_date: 2018-11-18 00:07:36
tags: [markdown,vscode,tools,development,blogging]
---

## TLDR

These are the tools I use to write this blog:

- **Editor**: [Visual Studio Code](https://code.visualstudio.com/)
- **Markup**: [Markdown](https://daringfireball.net/projects/markdown/)
- **Static Site Generator**: [Hexo](https://hexo.io/) 
- **Blog Theme**: [Cactus](https://github.com/probberechts/hexo-theme-cactus)
- **Hosting**: [Namecheap](https://www.namecheap.com/)

## Introduction

I have gotten a few questions regarding what I use to write this blog so I thought it would be a good idea to write a post about it. In this writeup I'll discuss what I use for writing posts as well as building and hosting the site. 

## Writing

### Editor

For writing, I use Visual Studio Code. Visual Studio Code is an open source, cross-platform text editor. One of the nice features it includes is Markdown Preview. Markdown Preview allows you to see how the content would look when rendered as a web page. Additionally, Visual Studio Code allows you to split panes. What that provides is the ability to write and preview the content side-by-side. 

![](/images/blog-tools/blog-tools-1.PNG)

### Markup 

I write my content in Markdown. Markdown is a plain text formatting syntax that allows authors to write easy-to-read and easy-to-write plain text formats that are then converted to HTML. It can take a while to get used to the syntax especially when including images, links and code snippets but once you get comfortable with it, it becomes natural and no different than writing sentences in plain text. While this is not a requirement for everyone, since a fair amount of my content includes code snippets, Markdown makes it easy to include such content in the language of choice. 

## Website

### Static Site Generation

While Markdown helps me format my plain text, at some point it needs to be converted into HTML or some format that can be rendered on the web. To help with that, I use Hexo. Hexo is an open source blog framework that allows you to write content using Markdown or other languages and generates static files which have a theme applied to them. Hexo is built on JavaScript, therefore you'll need NodeJS to configure it. 

### Theme 

Hexo has numerous themes that can be used to style your site. The one I use is the `Cactus` theme. 

## Hosting

For hosting, I use Namecheap. There is no special reason for this other than I had a slot available in an existing hosting plan. My method of deployment is via FTP. Since my site is all static assets, I upload them into a folder that is publicly accessible. With that in mind, by having my site made up of static assets I have the flexibility of hosting it on an S3 bucket or Azure Storage. This is the method I'm looking to use for deployments and hosting in the near future. Here is a [link](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website) that details how to host a static site on Azure Storage.