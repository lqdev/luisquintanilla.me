---
title: "Emacs Configuration"
language: "lisp"
tags: "elisp,emacs,org,linux,foss,editor,tools"
created_date: "2025-11-17 23:30 -05:00"
---

## Description

Emacs configuration files

## Usage

1. Create files and directories

    ```bash
    # Create the directory structure
    mkdir -p ~/.emacs.d
    mkdir -p ~/org/journal
    mkdir -p ~/.emacs.d/themes

    # Create your org files
    touch ~/org/inbox.org ~/org/todo.org ~/org/someday.org

    # Verify files were created
    ls -la ~/.emacs.d/
    ls -la ~/org/
    ```

1. Add init.el
## Snippet

### init.el

```elisp
;;; init.el --- Emacs initialization file
;;; Commentary:
;; Loads configuration from config.org

;;; Code:

;; Load literate configuration
(org-babel-load-file
 (expand-file-name "config.org" user-emacs-directory))

(provide 'init)
;;; init.el ends here
(custom-set-variables
 ;; custom-set-variables was added by Custom.
 ;; If you edit it by hand, you could mess it up, so be careful.
 ;; Your init file should contain only one such instance.
 ;; If there is more than one, they won't work right.
 '(package-selected-packages '(use-package)))
(custom-set-faces
 ;; custom-set-faces was added by Custom.
 ;; If you edit it by hand, you could mess it up, so be careful.
 ;; Your init file should contain only one such instance.
 ;; If there is more than one, they won't work right.
 )

;; Load Turquoise Highway theme
(add-to-list 'custom-theme-load-path "~/.emacs.d/themes/")
(load-theme 'turquoise-highway t)
```

### config.org

```org
#+TITLE: Emacs Configuration
#+AUTHOR: lqdev
#+PROPERTY: header-args :tangle yes

* About This Configuration
This is a literate Emacs configuration using org-mode. The code blocks are
tangled into init.el on startup. This approach keeps configuration documented
and maintainable.

* Startup Performance
Increase garbage collection threshold during startup for better performance.
We'll reset it after initialization.

#+begin_src emacs-lisp
(setq gc-cons-threshold (* 50 1000 1000))
#+end_src

* Package Management
** Setup Package Repositories
Configure package archives to use MELPA and GNU ELPA.

#+begin_src emacs-lisp
(require 'package)
(setq package-archives
      '(("melpa" . "https://melpa.org/packages/")
        ("gnu" . "https://elpa.gnu.org/packages/")))
(package-initialize)

;; Refresh package contents if archives are empty
(when (not package-archive-contents)
  (package-refresh-contents))
#+end_src

** use-package Setup
use-package provides a clean, declarative way to manage package configuration.

#+begin_src emacs-lisp
(unless (package-installed-p 'use-package)
  (package-refresh-contents)
  (package-install 'use-package))

(require 'use-package)
(setq use-package-always-ensure t)
#+end_src

* UI Configuration
** Basic UI Cleanup
Remove distracting UI elements for a cleaner editing experience.

#+begin_src emacs-lisp
(setq inhibit-startup-message t)
(menu-bar-mode -1)
(tool-bar-mode -1)
(scroll-bar-mode -1)

;; Better scrolling
(setq scroll-conservatively 100)
#+end_src

** Line Numbers
Show line numbers in programming modes.

#+begin_src emacs-lisp
(add-hook 'prog-mode-hook 'display-line-numbers-mode)
#+end_src

** Visual Preferences
#+begin_src emacs-lisp
;; Highlight current line
(global-hl-line-mode 1)

;; Show matching parentheses
(show-paren-mode 1)

;; Column number in mode line
(column-number-mode 1)
#+end_src

** Theme
Load the Turquoise Highway theme - desert nights, X-Files vibes.

#+begin_src emacs-lisp
(add-to-list 'custom-theme-load-path "~/.emacs.d/themes/")
(load-theme 'turquoise-highway t)
#+end_src

* Org Mode Configuration
** Directory Structure
Set up the basic three-file GTD system: inbox, todo, and someday.

#+begin_src emacs-lisp
(use-package org
  :config
  (setq org-directory "~/org/"
        org-default-notes-file (concat org-directory "inbox.org")
        org-agenda-files '("~/org/inbox.org" "~/org/todo.org"))
#+end_src

** TODO Keywords
Define workflow states for GTD-style task management.

#+begin_src emacs-lisp
  (setq org-todo-keywords
        '((sequence "TODO(t)" "NEXT(n)" "WAITING(w)" "|" "DONE(d)" "CANCELLED(c)")))
#+end_src

** Capture Templates
Quick capture templates for getting things into the inbox.

#+begin_src emacs-lisp
  (setq org-capture-templates
        '(("i" "Inbox" entry (file "~/org/inbox.org")
           "* TODO %?\n  %U\n")
          ("s" "Someday" entry (file "~/org/someday.org")
           "* %?\n  %U\n")))
#+end_src

** Refile Configuration
Set up refiling to move items between inbox, todo, and someday.

#+begin_src emacs-lisp
  (setq org-refile-targets
        '(("~/org/todo.org" :level . 1)
          ("~/org/someday.org" :level . 1)
          ("~/org/inbox.org" :level . 1)))
  (setq org-refile-use-outline-path 'file)
  (setq org-outline-path-complete-in-steps nil)
#+end_src

** Agenda Configuration
#+begin_src emacs-lisp
  (setq org-agenda-span 'week)
  (setq org-agenda-start-on-weekday 1) ; Start on Monday
)
#+end_src

** Daily Notes Function
Create a function to quickly open today's daily note in the journal directory.

#+begin_src emacs-lisp
(defun my/daily-note ()
  "Open or create today's daily note."
  (interactive)
  (let ((journal-dir (expand-file-name "journal" org-directory)))
    (unless (file-exists-p journal-dir)
      (make-directory journal-dir t))
    (find-file
     (expand-file-name
      (format-time-string "%Y-%m-%d.org")
      journal-dir))))
#+end_src

* Keybindings
** Org Mode Keys
Essential keybindings for the GTD workflow.

#+begin_src emacs-lisp
(global-set-key (kbd "C-c a") 'org-agenda)
(global-set-key (kbd "C-c c") 'org-capture)
(global-set-key (kbd "C-c d") 'my/daily-note)
(global-set-key (kbd "C-c l") 'org-store-link)
#+end_src

** General Navigation
#+begin_src emacs-lisp
(global-set-key (kbd "C-x C-b") 'ibuffer)
(global-set-key (kbd "M-o") 'other-window)
#+end_src

* Editing Enhancements
** Basic Settings
#+begin_src emacs-lisp
;; Always use spaces, never tabs
(setq-default indent-tabs-mode nil)

;; Show trailing whitespace
(setq-default show-trailing-whitespace t)

;; Auto-save and backup in one place
(setq backup-directory-alist '(("." . "~/.emacs.d/backups")))
(setq auto-save-file-name-transforms '((".*" "~/.emacs.d/auto-save-list/" t)))
#+end_src

** Better Defaults
#+begin_src emacs-lisp
;; Replace yes/no with y/n
(defalias 'yes-or-no-p 'y-or-n-p)

;; Automatically reload files when they change on disk
(global-auto-revert-mode 1)

;; UTF-8 everywhere
(set-language-environment "UTF-8")
(set-default-coding-systems 'utf-8)
#+end_src

* Performance Tuning
** Reset GC After Startup
Return garbage collection threshold to a reasonable value after startup.

#+begin_src emacs-lisp
(add-hook 'emacs-startup-hook
  (lambda ()
    (setq gc-cons-threshold (* 2 1000 1000))))
#+end_src

* Additional Packages (Optional)
** Magit
Uncomment if you want Git integration.

#+begin_src emacs-lisp :tangle no
(use-package magit
  :ensure t
  :bind ("C-x g" . magit-status))
#+end_src

** Which-Key
Shows available keybindings in a popup.

#+begin_src emacs-lisp :tangle no
(use-package which-key
  :ensure t
  :config
  (which-key-mode))
#+end_src

* End of Configuration
#+begin_src emacs-lisp
(message "Configuration loaded successfully!")
#+end_src
```

### turquoise-highway-theme.el

```elisp
;;; turquoise-highway-theme.el --- Desert nights terminal theme

(deftheme turquoise-highway
  "X-Files, late night desert driving vibes.")

(let ((bg "#0a0e14")
      (fg "#e6d5ac")
      (cursor "#ff6b35")
      (selection "#2d4a3e")
      (black "#1a1f29")
      (red "#d95757")
      (green "#4ec9b0")
      (yellow "#f0ad4e")
      (blue "#5b9aa9")
      (purple "#c586c0")
      (cyan "#7dd3c0")
      (white "#d4c5a9")
      (bright-black "#4a5568")
      (bright-red "#ff6b6b")
      (bright-green "#5eead4")
      (bright-yellow "#ffd166")
      (bright-blue "#73b3c4")
      (bright-purple "#e879f9")
      (bright-cyan "#06b6d4")
      (bright-white "#f5e6d3"))

  (custom-theme-set-faces
   'turquoise-highway

   ;; Basic faces
   `(default ((t (:foreground ,fg :background ,bg))))
   `(cursor ((t (:background ,cursor))))
   `(region ((t (:background ,selection))))
   `(mode-line ((t (:background ,bright-black :foreground ,fg))))
   `(mode-line-inactive ((t (:background ,black :foreground ,bright-black))))
   `(minibuffer-prompt ((t (:foreground ,cyan :weight bold))))
   `(fringe ((t (:background ,bg))))

   ;; Font-lock (syntax highlighting)
   `(font-lock-builtin-face ((t (:foreground ,cyan))))
   `(font-lock-comment-face ((t (:foreground ,bright-black :slant italic))))
   `(font-lock-constant-face ((t (:foreground ,purple))))
   `(font-lock-function-name-face ((t (:foreground ,blue))))
   `(font-lock-keyword-face ((t (:foreground ,green))))
   `(font-lock-string-face ((t (:foreground ,yellow))))
   `(font-lock-type-face ((t (:foreground ,bright-cyan))))
   `(font-lock-variable-name-face ((t (:foreground ,fg))))
   `(font-lock-warning-face ((t (:foreground ,red :weight bold))))

   ;; Line numbers
   `(line-number ((t (:foreground ,bright-black :background ,bg))))
   `(line-number-current-line ((t (:foreground ,cyan :background ,bg :weight bold))))

   ;; Links
   `(link ((t (:foreground ,blue :underline t))))
   `(link-visited ((t (:foreground ,purple :underline t))))

   ;; Org-mode
   `(org-level-1 ((t (:foreground ,bright-cyan :weight bold :height 1.2))))
   `(org-level-2 ((t (:foreground ,blue :weight bold :height 1.1))))
   `(org-level-3 ((t (:foreground ,green :weight bold))))
   `(org-level-4 ((t (:foreground ,yellow))))
   `(org-link ((t (:foreground ,blue :underline t))))
   `(org-todo ((t (:foreground ,red :weight bold))))
   `(org-done ((t (:foreground ,green :weight bold))))

   ;; Helm/Ivy (if you use them)
   `(helm-selection ((t (:background ,selection))))
   `(ivy-current-match ((t (:background ,selection :foreground ,bright-cyan))))

   ;; Highlight
   `(highlight ((t (:background ,selection))))
   `(hl-line ((t (:background ,black))))

   ;; Completions
   `(completions-common-part ((t (:foreground ,cyan))))
   `(completions-first-difference ((t (:foreground ,yellow :weight bold))))))

(provide-theme 'turquoise-highway)
```