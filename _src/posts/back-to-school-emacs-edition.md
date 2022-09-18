---
title: "Back To School: Emacs Edition"
published_date: 2017-12-09 13:01:19
tags: emacs,org-mode
---

As the summer comes to a close and I prepare to enter the fall semester, I am getting back into school mode. When programming in the terminal, I usually used vim. When compared to Emacs, the learning curve was relatively flat and it handled file editing just fine. I’ve read many articles that rave about Emacs and how powerful it can be. Therefore, a couple of months ago I took the plunge and decided to give it a try. Using dired mode to browse through the various directories in my system or project was a neat feature but for my purposes of basic file editing it did not offer anything that would warrant a switch. Furthermore, the default key bindings seemed a bit too much for me which caused me to  revert back to vim.

As part of the articles that I have read in praise of Emacs, one particular feature seemed to be a recurring theme – Org-Mode. Although I had heard about it, I never really understood what it was. Out of curiosity, I decided to try it out for myself and see what it had to offer. While taking notes in it seemed normal enough, it was the export functionality that truly rocked my world and made me realize how amazing this could be for note-taking. Traditionally a pen-and-paper note taker, one of the main reasons I dislike taking notes on my PC is the lack of continuity and organization. Although tools like OneNote of Evernote are extremely powerful, they were not exactly the best when it came to a semester’s worth of notes and although they could be made to have all of the notes for a semester on one page, this defeated the purpose of having different pages and notebooks. Enter Org-Mode. Again, while the process of taking notes is nothing special, the export capabilities are amazing. Furthermore, by default a table of contents is created with links so you can always find what you want when you want. Here are a few examples of what I mean.

![](/images/back-to-school-emacs-edition/backtoschoolemacsedition1.png)

As it can be seen, other than * or + characters in front of titles and lists, there is nothing special in the terminal. This is how it looks like in HTML format

![](/images/back-to-school-emacs-edition/backtoschoolemacsedition2.png)

Amazing! Not only do you get an organized set of notes, but you can see how this can easily contain a semester’s worth of notes in a single HTML file with main ideas being easily accessible via links. Now, maybe you don’t have an internet connection or prefer to put these notes on your tablet or phone. No problem. Just export it to a PDF.

![](/images/back-to-school-emacs-edition/backtoschoolemacsedition3.png)

Magic!

This is definitely going to take my note-taking to the next level by allowing me to have these documents synced across all my devices. Furthermore, it will keep notes organized and in one place. If you’re interested in getting started there may be just a few things that you need to do.

# Install Org-Mode

Org Mode is built in to the latest version of Emacs. However, in the event that it is not installed you can simply try the following commands in the Emacs mini buffer.

`M-x package-install RET org RET`

# Export to HTML

To access the export menu, simply type the following in the mini buffer:

`C-c C-e`

HTML export is included by default. To do that, we can add on to the command used to access the export menu as follows:

`C-c C-e h h`

PDF export is a bit mode complicated. The file is first exported to LaTeX format and then converted to PDF. However, in order to do this you need to install a package on your system. Assuming you are on a Unix machine, the following command should download everything necessary to convert LaTeX files to PDF. Keep in mind, this is relatively large and should take up roughly 1-2 GB in your system. In return, it limits the number of headaches due to installing necessary packages piecemeal that have multiple dependencies. To download the package, type in the following into your terminal:

# Install texlive-full

In the terminal, enter: 

```bash
sudo apt-get install texlive-full
```
Once installed, you can use a similar command to that of HTML export.

# Export to PDF

`C-c C-e l p`

This should be all you need to get started. For more information on how to use Org Mode, here’s a guide with live examples. Happy note-taking!

[Emacs Examples and Cookbook](http://home.fnal.gov/~neilsen/notebook/orgExamples/org-examples.html)

