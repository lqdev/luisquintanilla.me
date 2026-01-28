---
post_type: "article" 
title: "Generating Book Covers Using AI"
description: "How I used AI to generate book covers for Project Gutenberg books"
published_date: "2026-01-28 15:29 -05:00"
tags: ["ai","books","publicdomain","projectgutenberg"]
---

[Project Gutenberg](https://www.gutenberg.org/) is such a gem. There's so many books an entire lifetime still wouldn't be enough to read them all. The past few days, I've been downloading books from there and transfering them to my E-Reader.

One of the things I've noticed is that some books don't have cover images. Or better yet, they do but they're relatively simple. 

Take for example [Missing Link by Frank Herbert](https://www.gutenberg.org/ebooks/23210). This is what the current book cover looks like:

![Missing Link Project Gutenberg Simple Book Cover](https://github.com/user-attachments/assets/494d3e29-8fa7-4b12-9a7c-f33286aa4af6)

While this is fine, I wanted something more eye-catching to display on my E-Reader. So I decided to turn to AI for help.

Using ChatGPT's image generation capabilities, I provided the title, author, description which I took from the book's page on Project Gutenberg and the following prompt: 

> Can you generate a book cover image that includes the title, author name, and Project Gutenberg, and visually captures the themes of the story in a way that draws readers in?

This was the result:

![Missing Link Project Gutenberg AI Book Cover](https://github.com/user-attachments/assets/78c52c07-4a2a-435f-8f6f-8c91f1a56411)

I thought it was good, but the style was too modern. So I asked ChatGPT to refine the prompt and optimize it for image generation while taking into account the design aesthetic of the time (1950s).

This is what the optimized prompt looks like

> Generate a 1950s pulp science-fiction book cover for “Missing Link” by Frank Herbert, in the style of classic Astounding Science Fiction magazine covers. The illustration should be hand-painted and illustrative rather than photorealistic, with bold, slightly exaggerated forms and dramatic lighting.  
> <br>
> The scene depicts first contact on an alien jungle world: a human explorer in a mid-century space suit negotiating tensely with a reptilian alien holding advanced human technology salvaged from a crashed ship. The jungle should feel dense and exotic, with oversized alien foliage and a wrecked spacecraft partially visible.  
> <br>
> Use a limited, high-contrast color palette typical of 1950s pulp covers (vivid greens, oranges, yellows, deep blues). Typography should be bold, blocky, and vintage, with the title prominently at the top, the author’s name below it, and Project Gutenberg at the bottom.  
> <br>
> The overall tone should feel dramatic, mysterious, and slightly ominous, evoking Cold War–era anxieties, exploration, and the unknown — clearly recognizable as a mid-20th-century science fiction paperback cover.

The generated image from that prompt looks like this:

![Missing Link Project Gutenberg AI Book Cover](https://github.com/user-attachments/assets/244f9c49-2a8d-46dc-8dea-989249c76983)

Much better. I'm sure there's other ways I can keep refining the prompt and image endlessly but this is good enough for now. 

I have many other books that currently have the simple cover. Given I have several of them and I've validated the workflow, I think for the other books I'll have Copilot write a script to automate the image generation. Also, I don't know what Project Gutenberg's stance is on the use of AI, but if they're open to it, I'd be happy to donate / contribute the generated book covers. 

If I end up getting to the image generation scripts, I'll post more about that. Stay tuned!