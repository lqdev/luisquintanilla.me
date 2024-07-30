---
post_type: "note"
title: "User-provided target file names in Org Capture"
published_date: 2024-07-29 21:45 -05:00
tags: ["emacs","orgmode","capture","org","website","templates","gnu"]
---

I finally got my website note org-capture template working for new files.

Originally, I only had it working on existing files using this snippet.

```elisp
("wne"
 "Creates a note in an existing file"
 plain
 (file buffer-file-name)
 (file ,(file-name-concat website-template-dir "note.txt")))
```

This template uses the file name of the current buffer to select the insertion target.

Getting it to work with a new file specified by the user, requires a small tweak.

```elisp
("wnn"
 "Creates a note in a new file"
 plain
 (file (lambda () (file-name-concat website-note-dir (format "%s.md" (read-string "Enter file name: ")))))
 (file ,(file-name-concat website-template-dir "note.txt")))
```

At first, I thought I had to use the `function` target type since I wanted to use a function to capture the file name. That didn't work.

I then realized, I could still keep the `file` target type. However, to fill in the file name, I could use a function which takes in user input. 

Now that I got this working, I've also done the same for my reponse template. 
