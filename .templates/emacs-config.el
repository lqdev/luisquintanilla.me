;;; org-capture-templates.el --- Org-capture templates for lqdev.me site

;; Copyright (C) 2024

;; Author: Luis Quintanilla
;; URL: https://github.com/lqdev/luisquintanilla.me
;; Keywords: org, capture, templates
;; Version: 1.0

;;; Commentary:

;; This file provides org-capture templates for the lqdev.me static site.
;; These templates enable rapid content creation with consistent metadata
;; structure aligned with the F# Domain.fs type definitions.
;;
;; Installation:
;; 1. Copy this file to your Emacs configuration directory
;; 2. Update `lqdev-site-path` to point to your repository clone
;; 3. Add (load-file "~/.emacs.d/org-capture-templates.el") to your init.el
;; 4. Or copy the configuration directly into your init.el
;;
;; Usage:
;; Press C-c c (or your configured keybinding), then:
;; - l → category → specific type
;; Example: C-c c l n n (Create note)
;;          C-c c l r b (Create bookmark)

;;; Code:

;; Load org-capture
(require 'org-capture)

;; Define the path to your repository (CHANGE THIS!)
(setq lqdev-site-path "~/path/to/luisquintanilla.me/")

;; Define org-capture templates for lqdev.me site
(setq org-capture-templates
      '(
        ;; Main content types hierarchy
        ("l" "lqdev.me Content")
        
        ;; Posts (lp*)
        ("lp" "Post Types")
        ("lpa" "Article" plain
         (file (lambda () (concat lqdev-site-path "_src/posts/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/article.txt")))
         :empty-lines-after 1)
        
        ("lpn" "Note" plain
         (file (lambda () (concat lqdev-site-path "_src/notes/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/note.txt")))
         :empty-lines-after 1)
        
        ;; Media (lm*)
        ("lm" "Media Types")
        ("lmm" "Media Post" plain
         (file (lambda () (concat lqdev-site-path "_src/media/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/media.txt")))
         :empty-lines-after 1)
        
        ("lma" "Album Collection" plain
         (file (lambda () (concat lqdev-site-path "_src/albums/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/album-collection.txt")))
         :empty-lines-after 1)
        
        ("lmp" "Playlist Collection" plain
         (file (lambda () (concat lqdev-site-path "_src/playlists/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/playlist-collection.txt")))
         :empty-lines-after 1)
        
        ;; Responses (lr*)
        ("lr" "Response Types")
        ("lrr" "Response" plain
         (file (lambda () (concat lqdev-site-path "_src/responses/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/response.txt")))
         :empty-lines-after 1)
        
        ("lrb" "Bookmark" plain
         (file (lambda () (concat lqdev-site-path "_src/bookmarks/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/bookmark.txt")))
         :empty-lines-after 1)
        
        ;; Resources (lo*)
        ("lo" "Resources")
        ("lov" "Review" plain
         (file (lambda () (concat lqdev-site-path "_src/reviews/library/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/review.txt")))
         :empty-lines-after 1)
        
        ("los" "Snippet" plain
         (file (lambda () (concat lqdev-site-path "_src/snippets/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/snippet.txt")))
         :empty-lines-after 1)
        
        ("low" "Wiki" plain
         (file (lambda () (concat lqdev-site-path "_src/wiki/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/wiki.txt")))
         :empty-lines-after 1)
        
        ("lop" "Presentation" plain
         (file (lambda () (concat lqdev-site-path "_src/resources/presentations/" 
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/presentation.txt")))
         :empty-lines-after 1)
        
        ("lol" "Livestream" plain
         (file (lambda () (concat lqdev-site-path "_src/streams/" 
                                  (format-time-string "%Y-%m-%d-")
                                  (read-string "Filename slug: ") ".md")))
         (file (lambda () (concat lqdev-site-path ".templates/livestream.txt")))
         :empty-lines-after 1)
        ))

;; Optional: Set a global keybinding for org-capture
(global-set-key (kbd "C-c c") 'org-capture)

(provide 'org-capture-templates)

;;; org-capture-templates.el ends here
