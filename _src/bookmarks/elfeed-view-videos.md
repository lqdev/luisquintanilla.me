---
title: "Using Elfeed to view videos"
targeturl: https://medium.com/emacs/using-elfeed-to-view-videos-6dfc798e51e6 
response_type: bookmark
dt_published: "2022-12-02 21:58"
dt_updated: "2022-12-02 21:58 -05:00"
---

Slightly modified the original script to use [Streamlink](https://streamlink.github.io/) and lower quality to 240p for bandwith and resource purposes.

```lisp
(require 'elfeed)

(defun elfeed-v-mpv (url)
  "Watch a video from URL in MPV" 
  (async-shell-command (format "streamlink -p mpv %s 240p" url)))

(defun elfeed-view-mpv (&optional use-generic-p)
  "Youtube-feed link"
  (interactive "P")
  (let ((entries (elfeed-search-selected)))
    (cl-loop for entry in entries
     do (elfeed-untag entry 'unread)
     when (elfeed-entry-link entry) 
     do (elfeed-v-mpv it)) 
   (mapc #'elfeed-search-update-entry entries) 
   (unless (use-region-p) (forward-line)))) 

(define-key elfeed-search-mode-map (kbd "v") 'elfeed-view-mpv)
```
