---
post_type: "note" 
title: "Clock Tables - Org Mode, Plain Text, and AI"
published_date: "2024-07-09 22:07"
tags: ["emacs","orgmode","ai","plaintext","productivity","tools","technology","gnu","opensource","gtd","calendar","agenda","llm","openai"]
---

Org-mode appreciation post. 

I use plain text and org-mode for most things in my life, especially when it comes to task and life management. 

I won't rehash all the resons Emacs and org-mode are amazing. There are tons of blog posts and videos out there that would do it more justice than I ever could. 

Over the last few years, Emacs has become my go-to text editor. Yet despite all the time, I keep finding new features that delight. 

The most recent is [clock table](https://orgmode.org/manual/The-clock-table.html).

I already use org-mode to track my to-dos and perform some sort of time-block planning by setting deadlines and scheduling tasks. 

Recently though, I've been wanting a way to see all of the things I've worked on over the past [ INSERT TIME PERIOD ]. More importantly, I'd like to have time associated with them to see where my time has gone and evaluate whether I'm spending time on the things I should be. 

I knew you could [clock in and clock out](https://orgmode.org/manual/Clocking-commands.html) on tasks. However, I didn't know you could easily build a customized report that automatically updates. That's when I came across clock tables. 

Now, I have a way of visualizing all of the things I worked on during a week or month, and as I'm planning for the next week or month, I can adjust and reprioritize the things I'm working on.

I know there are enterprise offerings like the Viva suite from Microsoft which provides detailed reports on how you spend your time. 

What excites me about org-mode though is that it's plain text. The clock table report that gets generated is a plain text table which makes it portable and easy to access using any text editor of your choice. It works best with Emacs, but that's not a requirement. 

On their own, clock tables are amazing. 

However, given how well language models work on plain text, they could be used as context for your queries. Imagine giving a language model as input an org file which contains:

- A clock table
- A list of TODO tasks (with notes, priorities, deadlines, tags, properties, and other annotations)
- A high level list of goals you want to achieve

Then, you could enter a prompt along the lines of: "Using the following clock-table and list of goals I want to achieve, provide me with recommendations of tasks I should work on for the next week. Ensure that they align with my goals, are top priority, and provide the highest return on my efforts".

Additionally, you might also provide your org-agenda view containing appointments and use the results from the first query as context for the following prompt: "Given the agenda view for the next week, schedule the top 3 tasks you recommended". 

The result would be a list of TODO items containing schedule / active timestamps annotations which now show up on your org-agenda view. 

Today, almost every productivity application out there is working on building these kinds of AI features into their products. The difference is, in many cases, the formats used by those applications aren't plain text. This adds complexity to the data extraction and transformation pipelines required to get the data into the right format for a language model to process. With plain text, there's little to no transformations required. 

What's even better, I can extend org-mode and Emacs using elisp to do the things I just talked about. 

I'm no elisp expert, so I asked Copilot to generate an elisp function that takes an org-mode clock table as input and generates a written text summary of it. Here are the results:

```lisp
(defun org-clock-summary-to-chatgpt ()
  "Extracts clock table data and requests a summary from ChatGPT."
  (interactive)
  (let* ((clock-table-data (org-clocktable-get-clocktable))
         (summary-text (org-clocktable-format-summary clock-table-data))
         (api-endpoint "https://api.openai.com/v1/engines/davinci/completions")
         (api-key "YOUR_API_KEY")) ; Replace with your actual API key

    ;; Construct the HTTP request and send it to ChatGPT
    (with-current-buffer
        (url-retrieve-synchronously api-endpoint
                                    `(("text" . ,summary-text)
                                      ("api_key" . ,api-key)))
      (goto-char (point-min))
      (search-forward "\n\n")
      (let ((response-json (json-read)))
        (message "ChatGPT summary: %s" (cdr (assoc 'choices response-json)))))))
```

All you elisp experts out there, let me know how it did. 

I know at least the API endpoint is wrong, but generally speaking, it seems to be doing the right thing. Such function could be extended to include org-agenda information, TODO items, and many other things that would use AI to augment the existing functionality of Emacs and org-mode to tailor them to my needs. 