---
name: research-brief
description: Use when asked to research a technology, standard, protocol, article, or announcement and say what it means for the project, for example "take a look at this and tell me how it affects us", "how might I implement this", or "do some research, use web search". Explains the idea first, maps it to what the project already does, prefers existing standards, and separates documented facts from measurements and opinion.
---

# Write a research brief

## Steps

1. Read the thing itself first: the spec, the post, the repository. Not a summary of it.
2. Explain the idea in plain words before anything about this project. Assume the reader hasn't read it.
3. Find what the project already does that is close. Name the files. Read them; don't guess.
4. Look for an existing standard or convention before proposing anything new. If one exists, say which, and who else has implemented it.
5. List the options, including "do nothing". For each: what it takes, what it gives, what it risks.
6. Recommend one, and say what would change your mind.
7. Check your own report. For each claim that matters, open the source again and confirm the wording. Remove or mark anything you couldn't confirm.

## Output

```text
## The idea
## What we already have
## Existing standards and prior art
## Options
| Option | What it takes | What it gives | Risk |
## Recommendation
## What I couldn't verify
## Sources
```

Mark each important claim as [documented] (a spec or vendor doc says so), [measured] (a study or your own test), or [opinion] (someone's advice). Give the URL and the date of each source.

## Gotchas

- Don't invent a standard. If nothing fits, say that, and propose the smallest extension of the nearest one.
- Search results and other models' summaries are leads, not sources. Open the page.
- Say when sources disagree, and which you trust and why.
- "I couldn't find it" is a finding. Write it down instead of filling the gap.
- This is research. Don't change files or open a pull request unless asked.
