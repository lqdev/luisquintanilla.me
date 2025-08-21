---
post_type: "article" 
title: "Manage your reading list in Emacs using Org mode"
description: "Learn how to use TODO items in Emacs Org mode to manage your reading list"
published_date: "07/26/2022 19:15 -05:00"
tags: [linux, emacs, organization]
---

## Introduction

Emacs is a rich and customizable text editor. [Org mode](https://orgmode.org/) for Emacs makes it easy to manage to-do lists, projects, and author technical documents. Many of my workflows like writing, planning, and organization make heavy use of plain text. One reason I enjoy working in plain text is, it's easy for my thoughts and ideas to naturally flow without having tools get in the way. Another reason I like plain text is, I'm not locked-in to any tool. Because plain text can be authored and rendered almost anywhere, it's easy for me to use whichever tool I find most productive. Some that I've found great success with recently have been [Visual Studio Code](https://code.visualstudio.com/), [Joplin](https://joplinapp.org/), and Emacs with Org mode. I've been trying to manage my reading list in plain text and while it's worked well, I'd like a way to track the state for each book by tagging them as read or in various stages of unread. One of the features of Org mode is the ability to track workflows. In this guide, I'll show how you can extend the TODO Org mode functionality to keep track of your reading list.

## Prerequisites

- [Emacs](https://www.gnu.org/software/emacs/)

Since Emacs is cross-platform, the contents of this guide should work the same across Linux, Mac, and Windows.

## Create org file

Org mode in Emacs provides to-do list management features. By default, the built-in states are TODO and DONE. However, you can define your own states to best match your workflow. For more information, see the [TODO item section in the Org mode guide](https://orgmode.org/manual/TODO-Items.html#TODO-Items).

Start by creating a new file in Emacs with the *.org* file extension (i.e. reading-list.org).

Then, add the following headers at the top of the file.

```text
#+TODO: WISHLIST(w) QUEUED(q) INPROGRESS(p) | READ(r!)
```

What this heading does is, it defines the different states of completion of a TODO item. The scope of this definition is the file. Although you can define global states, I want these states to only exist in the context of this file. The complete and incomplete states are separated by the pipe character (`|`). In this case, WISHLIST, QUEUED, and INPROGRESS are incomplete states and READ is the complete state. The letters in the parentheses are ways to quickly assign each of the states. One last thing you'll notice is the READ state in addition to having a letter also has an exclamation point. The exclamation point tells Emacs to add a timestamp whenever this book transitions to the READ state.

## Add your book list

Once you've define your reading states, it's time to add your books. TODO items in Emacs Org-mode only work in the context of headings. Therefore you have to prefix your books with asterisks. For more information on headlines, see the [Headlines section in the Org mode guide](https://orgmode.org/manual/Headlines.html). 

Here are examples of some books in my reading list.

```text
* Hackers (Steven Levy)
* Ghost In The Wires (Kevin Mitnick)
```

Since I don't intend to have sub headlines, I just used a top-level headline for each of the books. 

## Assign state

Now that you've populated your list of books, it's time to assign states to them. 

To assign a state:

1. Place your pointer / cursor on the same line as the book you want to assign the state to.
1. Press `Ctrl-c` + `Ctrl-t` or in Emacs notation `C-c C-t`.
1. A buffer opens prompting you to choose a state. Enter one of the letters to choose your state. For example, if you want to apply the QUEUED state to your book, press the letter `q`. 

The state you selected is prepended to your book. For example:

```text
* QUEUED Hackers (Steven Levy)
```

## Update state

To update the state, you can follow the steps in the assign state section or you can cycle through the various states by pressing `Alt-Left` or `Alt-Right` or in Emacs notation `M-LEFT` or `M-RIGHT`. 

## Marking a book as READ

To assign the READ state to a book, you need to update its state. The only difference between READ and the other states is, a timestamp is applied when the book transitions to the state. 

Here's an example of what a book in the READ state looks like:

```text
* READ In The Beginning...Was The Command Line (Neal Stephenson) 
- State "READ"       from "INPROGRESS" [2022-07-26 Tue 00:18]
```

## Querying your reading list

Org mode has agenda views that allow you to view TODO, timestamped, and tagged items in a single place. One of the built-in agendas that is useful for querying TODO items is the match view. The match view shows headlines based on the tags, properties, and TODO state associated with them. For more information, see the [Agenda views section in the Org-mode guide](https://orgmode.org/manual/Agenda-Views.html). 

Before viewing your books in an agenda view, you need to add the file containing your reading list to the list of files tracked by Org. To do so, open the file in Emacs and press `Ctrl-c + [` or in Emacs notation `C-c [`.

To launch the match agenda view in org-mode:

1. Press `Alt-x` or in Emacs notation `M-x`
1. In the minibuffer, type `org-agenda` and press Enter.
1. A buffer appears prompting you to select a view. Press the letter `m` to choose the match view. 

### Unread items

In this case, I want to see all the unfinished books which in this case are under the WISHLIST, QUEUED, and INPROGRESS states. 

In the match view, enter the following query into the minibuffer:

```text
TODO="WISHLIST"|TODO="QUEUED"|TODO="INPROGRESS"
```

Press `Enter`.

The result of the query should look similar to the following:

```text
Headlines with TAGS match: TODO="WISHLIST"|TODO="QUEUED"|TODO="INPROGRESS"
Press ‘C-u r’ to search again
  reading-list:QUEUED Hackers (Steven Levy)
  reading-list:WISHLIST Ghost In The Wires (Kevin Mitnick)
```

### Read items

To query for read items, you go through the same process, except the query is different.

In the match view, enter the following query into the minibuffer:

```text
TODO="READ"
```

Press `Enter`.

The result of the query should look similar to the following:

```text
Headlines with TAGS match: TODO="READ"
Press ‘C-u r’ to search again
  reading-list:READ In The Beginning...Was The Command Line (Neal Stephenson)
```

## Conclusion

Reading lists are just one of the many things you can track using Emacs and Org mode. You can use this general process to track movies, TV shows, podcasts, etc. Thanks to the extensibility of Emacs, the options are endless!