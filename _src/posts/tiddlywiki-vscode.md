---
post_type: "article" 
title: "Get started with TiddlyWiki and VS Code"
description: "Downlaod and set up TiddlyWiki for use in VS Code"
published_date: "2022-10-21 08:32"
tags: web,vscode,wiki,personalknowledgemanagement,git,website
---

## Introduction

A while back, I set up a knowledge base (KB) section on my website. As part of that knowledge base, I have a [wiki](/wiki) and [snippets](/snippets) section. As opposed to the [blog posts](/posts) which tend to be more long-form and have structure or the microblog-like posts on my [main](/feed) and [response](/feed/responsts.html) feeds, the wiki and snippets section are intended to have self-contained blocks of information that are linked somehow but not a linear or chronological order. 

My current wiki and snippets solution was fairly basic. It was a series of documents authored in Markdown which then get converted to static HTML pages. The index pages for wiki articles and snippets contain links to each of those documents. While that's worked okay, I felt I was missing functionality when it came to linking between documents, tagging, and more importantly searching. 

While I had seen wiki solutions like [MediaWiki](https://www.mediawiki.org/wiki/MediaWiki), they felt too bloated for my use case. Since my site is statically hosted, I didn't want to have to create, maintain, and pay for a server only to run my wiki. That's where TiddlyWiki comes in.

[TiddlyWiki](https://tiddlywiki.com/) describes itself as "a unique non-linear notebook for capturing, organising and sharing complex information". In short, TiddlyWiki is a self-contained single HTML file with WYSIWYG-like functionality for creating a wiki. Because everything is contained in a single HTML file, it's static-site friendly. 

Now I had heard of TiddlyWiki before but dismissed it because I didn't get how it worked. For one, I was confused how the WYSIWYG GUI supported editing, viewing, and exporting of these notes since there was just a single HTML file. More importantly, when it comes to [saving changes](https://tiddlywiki.com/#Saving), the recommendations on the website point users to browser extensions and apps. If it's just an HTML page, why can't I use the tools I'm already using to work with HTML? 

After giving it another look and tinkering a bit, I found I was right and could use tools like VS Code to work with TiddlyWiki. In this post, I'll show how you can get started with the empty edition of TiddlyWiki using VS Code and the Live Preview extension.  

## Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/#alt-downloads)
- [Live Server Extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.live-server)

This guide was written on a Linux (Manjaro) PC but should work the same on Windows and macOS.

## Download TiddlyWiki

The quickest way to get started with TiddlyWiki is to: 

1. Download the [empty edition](https://tiddlywiki.com/#Empty%20Edition). 

    A file called *empty.html* should download to your computer. For simplicity, I would recommend creating a new directory on your computer that only contains that file.

    The empty edition contains the core functionality. For other editions, see [TiddlyWiki editions](https://tiddlywiki.com/#Editions).

1.  Rename the file *index.html*.

## Edit and save your wiki

### Edit your wiki

1. Open the directory containing your *index.html* file in VS Code.
1. Open the command palette (View > Command Palette).
1. Enter into the text box *Live Preview: Start Server* and press *Enter*. The server should start.

![lqdev TiddlyWiki Main Page](/images/tiddlywiki-vscode/lqdev-tiddlywiki.png)

### Save your wiki

Saving can be a bit tricky but it's not too bad. Use the checkmark in the side pane to save your changes. When there are unsaved changes, the checkmark turns red. 

![TiddlyWiki Side Panel Red Checkmark Save Changes](/images/tiddlywiki-vscode/tiddlywiki-save-changes.png)

To save your changes, overwrite the existing file or if you want, you can also save it to a file with a different name. I would recommend saving it to the same file because it's easier to manage. You don't want to end up with *index-version12341.html*. That being said, I don't know if there may be risks in overwriting the file. In my limited testing I had no issues overwriting the file, but I'd still tread lightly.

### Stop the server

Once you're done viewing and editing your wiki, stop the Live Preview server:

1. In VS Code, open the command palette **(View > Command Palette)**.
1. Enter into the text box *Live Preview: Stop Server* and press *Enter*. The server should stop.

## Tips

### Plugins

I prefer working in Markdown and since some of my notes include code, I addded two plugins to my wiki:

- [Markdown](https://tiddlywiki.com/#Markdown%20Plugin)
- [Highlight](https://tiddlywiki.com/#Highlight%20Plugin)

For more information, see [Installing a plugin from the plugin library](https://tiddlywiki.com/#Installing%20a%20plugin%20from%20the%20plugin%20library).

### Things to watch out for in VS Code

When using the Live Preview extension, some things might not work such as keyboard shortcuts and navigating to external links. Since a local server is running you should be able to use the browser as well. However, I would recommend only using the browser to *view* your wiki. Trying to save changes made in the browser will download the HTML page containing your changes. 

### Use Git

Autosaving to Git is one of the ways you can save your TiddlyWiki. However, you have to enter and use your credentials. Since your wiki is a static HTML page, just save it like you would any other HTML file in a repo. Doing so also makes it easy to backup to a previous version in case data is corrupted or lost. 

## Conclusion

I this post, I shared how TiddlyWiki provides a relatively low overhead, static-website friendly way of creating wikis. If you use VS Code, you can use the Live Preview extension to view and edit your wiki. Although my intended use for TiddlyWiki is as a personal knowledge management solution, I can also think of ways you could use TiddlyWiki as a low-friction way of building your own website. The WYSIWYG functionality makes creating content extremely more accessible than static site generators and similar to WordPress. In contrast to WordPress though, since you only have a single HTML file, it's less overhead to manage and also low-cost since you can [host it on GitHub Pages for free](https://nesslabs.com/tiddlywiki-static-website-generator) or any other file hosting service. In the near future, I plan to migrate my wiki and snippets to use TiddlyWiki. 