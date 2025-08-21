---
post_type: "article" 
title: Working with URLs in Racket
published_date: 2017-12-13 16:01:57 -05:00
tags: [lisp, racket]
---

Often times when working with web applications, parameters and values are passed in the path as well as the query string of a URL. In order to do something useful, parsing of the url is required. Noted below are examples of how this can be done in an elegant manner using Racket.

The first step is to import the appropriate packages

```lisp
(require net/url)
(require net/url-structs)
```

Assuming we are working with the url string "https://hacker-news.firebaseio.com/v0/item/192327.json?print=pretty", we can convert the string to a URL struct using the `string->url` procedure.

```lisp
(define myurl (string->url "https://hacker-news.firebaseio.com/v0/item/192327.json?print=pretty"))
```

The result is the following output
```lisp
myurl
;;(url
;; "https"
;; #f
;; "hacker-news.firebaseio.com"
;; #f
;; #t
;; (list
;;  (path/param "v0" '())
;;  (path/param "item" '())
;;  (path/param "192327.json" '()))
;; '((print . "pretty"))
;; #f)
```

The structure of the URL struct is as follows and directly maps to the values displayed above.

```lisp
(struct url ( scheme
 	      user
 	      host
 	      port
 	      path-absolute?
	      path
 	      query
 	      fragment)
    #:extra-constructor-name make-url)
  scheme : (or/c false/c string?)
  user : (or/c false/c string?)
  host : (or/c false/c string?)
  port : (or/c false/c exact-nonnegative-integer?)
  path-absolute? : boolean?
  path : (listof path/param?)
  query : (listof (cons/c symbol? (or/c false/c string?)))
  fragment : (or/c false/c string?)
```

To access the individual data members of the struct, the syntax `<type>-<member> <instance>`. What is returned can be further manipulated based on its type (string, list, boolean)

```lisp
(url-host myurl)
;;"hacker-news.firebaseio.com"
```

###### Source
[URLS and HTTP](https://docs.racket-lang.org/net/url.html)