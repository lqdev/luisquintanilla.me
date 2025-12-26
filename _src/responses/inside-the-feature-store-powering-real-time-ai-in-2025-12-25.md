---
title: "Inside the feature store powering real-time AI in Dropbox Dash"
targeturl: https://dropbox.tech/machine-learning/feature-store-powering-realtime-ai-in-dropbox-dash
response_type: reshare
dt_published: "2025-12-25 23:14 -05:00"
dt_updated: "2025-12-25 23:14 -05:00"
tags: ["ai","dropbox","contextengineering","featureengineering"]
---

> [Dropbox Dash](https://www.dash.dropbox.com/) uses AI to understand questions about your files, work chats, and company content, bringing everything together in one place for deeper, more focused work. With tens of thousands of potential work documents to consider, both search and agents rely on a ranking system powered by real-time machine learning to find the right files fast. At the core of that ranking in Dash is our feature store, a system that manages and delivers the data signals (“features”) our models use to predict relevance.  
> <br>
> To help users find exactly what they need, Dash has to read between the lines of user behavior across file types, company content, and the messy, fragmented realities of collaboration. Then it has to surface the most relevant documents, images, and conversations when and how they’re needed. The feature store is a critical part of how we rank and retrieve the right context across your work. It’s built to serve features quickly, keep pace as user behavior changes, and let engineers move fast from idea to production. (For more on how feature stores connect to context engineering in Dash, check out our [deep dive on context engineering right here](https://dropbox.tech/machine-learning/how-dash-uses-context-engineering-for-smarter-ai).)  
> <br>
> In this post, we’ll walk through how we built the feature store behind Dash’s ranking system, why off-the-shelf solutions didn’t fit, how we designed for speed and scale, and what it takes to keep features fresh as user behavior changes. Along the way, we’ll share the tradeoffs we made and the lessons that shaped our approach.