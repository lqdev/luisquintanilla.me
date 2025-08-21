---
title: "How to implement Q&A against your documentation with GPT3, embeddings and Datasette"
targeturl: http://simonwillison.net/2023/Jan/13/semantic-search-answers/#atom-everything 
response_type: bookmark
dt_published: "2023-01-19 14:19 -05:00"
dt_updated: "2023-01-19 14:19 -05:00"
---

> Here's how to do this:  
>                                                                                         
> - Run a text search (or a semantic search, described later) against your documentation to find content that looks like it could be relevant to the user's question.                                             
> - Grab extracts of that content and glue them all together into a blob of text.
> - Construct a prompt consisting of that text followed by "Given the above content, answer the following question: " and the user's question                                                                                  
> - Send the whole thing through the GPT-3 API and see what comes back  