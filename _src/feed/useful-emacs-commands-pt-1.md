---
post_type: "note"
title: "Note to Self: Useful Emacs Commands Pt. 1"
published_date: 2024-07-28 17:00 -05:00
tags: ["emacs","lisp","elisp","programming","code","notetoself"]
---

I still need to set up an org-capture template for snippets, so writing a note to myself to remember these commands I'll eventually want to come back to.

## Capture elfeed link

In this custom function, `elfeed-show-yank` extracts the link element in an elfeed entry. `org-capture` then just invokes the org-capture template selection prompt. At this point, I can move forward with creating a response entry on the website and since the link to the entry I was viewing in elfeed is in the kill-ring, I can easily paste the URL while filling out the org-capture template. 

```lisp
(defun capture-elfeed-entry ()
  (elfeed-show-yank)
  (org-capture))
```

## Org capture contexts

I recently found out, I can add filters to org-capture templates based on the mode I'm in Emacs. Here's an example:

```lisp
(setq org-capture-templates-contexts
      '(("wrn" ((in-mode . "elfeed-show-mode")))))
```

`org-capture-templates-contexts` defines a set of conditions that defines which contexts certain org-capture templates appear under.

For example, the org-capture template mapped to `wrn` will only be visible when org-capture is invoked from a buffer in `elfeed-show-mode`.

More information can be found in the [org-capture templates in context documentation](https://orgmode.org/manual/Templates-in-contexts.html)
