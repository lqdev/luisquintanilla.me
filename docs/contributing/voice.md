# Voice

How prose in this repo should sound, and how to tell when it doesn't.

## The reference is Luis's own unassisted writing

The baseline is the 67 blog posts under `_src/posts/` dated 2017 to 2024 (63,400 words). Three 2026
posts say "AI was used to help me organize and improve the flow of this post", so they are not part
of the baseline. Posts from 2025 on are also left out, because their formatting shifts (four times
the bold) and I can't tell why.

Measured per 1,000 words, code blocks and link targets removed:

| Mark | Luis's posts, 2017 to 2024 | Repo docs on 2026-11-23 |
| --- | --- | --- |
| Em dashes | 0.05 | 2.76 |
| Bold spans | 1.45 | 45.7 |
| Bullets that open with a bold label | 0.38 | 12.61 |
| Emoji and status symbols | about 0 | 10.86 |
| "I" and "my" | 16.9 | 0.4 |
| Average sentence length | 17.4 words | not measured |

The docs use em dashes 55 times as often as he does and bold 31 times as often, and they almost
never say "I". That gap is what this guide closes.

## What his writing does

Each trait has a quote from a post or note so you can hear it.

1. He writes in the first person and says why he personally did something.
   "I don't use platforms like Google Analytics to track visitors. I used to self-host a GoatCounter
   instance but personally I don't really care about traffic so I got rid of that." (2023)
2. He starts from his own situation, then gets to the technology.
   "Since it was a holiday weekend in the U.S. that kind of snuck up on me, I found myself with
   nothing planned on a Saturday." (2025)
3. He tells you what the page will do, plainly.
   "In this post, I'll go over a lightweight implementation for sending Webmentions using F# and
   test it using the webmention.rocks website." (2021)
4. He walks the reader through it with "you" and "let's".
   "Let's say you want to use webmentions to reply to or mention content from another site. The
   general workflow works as follows:" (2021)
5. He gives the number.
   "About 20GB of disk space on my instance are being taken up by media from remote users. And this
   is for a single user instance!" (note)
6. He admits what he doesn't know or got wrong, and he hedges when he is unsure: "I believe",
   "kind of", "in my opinion".
   "How I integrate it into my website is a future me problem." (2025)
7. He links a lot: to sources, to other people's work, and to his own earlier posts.
8. His sentences run medium to long and are joined with "and", "so", "but", "because". He opens
   sentences with "So", "But", "Therefore", and "Instead of".
   "Instead of building new custom functions, I can repurpose them and apply additional
   transformations to compute aggregate statistics." (2023)
9. He ends short and plain.
   "Overall, I'm happy with the end result." (2025) "Ultimately though, the choice is yours." (2021)
10. He uses everyday idioms: "don't let perfect be the enemy of good", "back to the drawing board",
    "rabbit hole", "can't come soon enough".
11. Formatting is light: plain headings, little bold, straight quotes, almost no em dashes, at most
    one emoji in a casual post and none in technical ones.

Don't copy his typos, and don't copy his chat register ("Alright friend", "Hey bud"). That is how he
talks to an assistant, not how he writes for readers.

## How the voice applies to each kind of page

| Page | Person | Notes |
| --- | --- | --- |
| `README.md`, [principles](../principles.md) | "I" | These are his claims and his reasons. Use his words from the interview. |
| [Getting started](../getting-started.md), guides | "you", with "I" for his own setup | Walk the reader through it. Give the command, then what they should see. |
| [Architecture](../architecture.md), feature pages | "I" for decisions, plain description for mechanics | "I put content-type identity in one file because…" then describe the file. |
| Reference pages | none | Tables and exact values. No persona, no adjectives. |
| `AGENTS.md`, instruction files, skills | imperative; "I" only in the working agreements | Written for agents. Short. Still no marks from the list below. |

## Signs of AI writing to remove

Sources: [Wikipedia: Signs of AI writing](https://en.wikipedia.org/wiki/Wikipedia:Signs_of_AI_writing)
and [Clarity](https://clarity.addy.ie/). These are the ones that show up in this repo's docs.

Checked by the integrity test (check 7), so the run fails:

- Em dashes. Use a comma, a colon, parentheses, or two sentences.
- Curly quotes. Use straight ones.
- Emoji and status symbols, including in headings.
- Words and phrases on the cut list: `delve`, `leverage`, `seamless`, `robust`, `comprehensive`,
  `genuinely`, `pivotal`, `testament`, `underscore`, `foster`, `showcase`, `tapestry`, `elevate`,
  `empower`, `game-changer`, `deep dive`, `production-ready`, `cutting-edge`, `state-of-the-art`,
  `world-class`, `best-in-class`, `effortless`, `research-backed`, `successfully implemented`,
  `it's worth noting`, `it is important to note`, `in conclusion`, `more than just`,
  `not just X but Y`, `plays a key role`.

Reported as warnings, so a person decides:

- A bullet that opens with a bold label followed by a colon. Write a sentence, or use a table.
- More than 8 bold spans per 1,000 words.

Not checkable by script. Read for these:

- Claims of importance with nothing behind them: "a critical pattern", "dramatically improves".
  Say what happened and give the number, or cut the sentence.
- A trailing "-ing" phrase that adds a vague benefit: `…, ensuring consistency across the site.`
  Cut it, or say what specifically stays consistent and how.
- Lists of three by reflex: `fast, flexible, and reliable`. Keep the items that are true and specific.
- "It isn't X. It's Y." Just say Y.
- Section-ending summaries and morals. Stop when the content stops.
- Headings in Title Case. Use sentence case.
- Status banners and victory language: `Status: Complete`, `successfully`, "now includes".
  Docs describe what is, in the present tense.
- The same thing called by three names for variety. Pick one name and keep it.
- Chat leftovers: `Certainly!`, `I hope this helps`, `Here's a breakdown`.
- Paragraphs that could appear word for word in another project's docs. Replace them with something
  only this repo could say: a file name, a number, a reason, a thing that went wrong.

One caution from Clarity: don't strip every hedge. Luis hedges when he is unsure, and removing honest
uncertainty makes the text sound less like him.

## A before and after

From the current README:

```text
- **Progressive Loading**: Handles 1000+ content items without performance issues
- **Desert Theme Integration**: Modern responsive design with IndieWeb microformats2
```

Problems: bold-label bullets, an unbacked claim ("without performance issues"), filler adjectives
("modern"), no reason, no person.

Rewritten:

```text
The home page lists about 1,900 posts. Putting them all in the HTML broke the browser's parser, so
the build writes them to JSON and the page loads them in chunks as you scroll.
```

## Review steps for any page you write or edit

1. Run `dotnet fsi test-scripts/test-docs-integrity.fsx` and clear every check 7 failure in the file.
2. Read the warnings and decide each one.
3. Read the page aloud. Fix every sentence you wouldn't say to a colleague.
4. For each paragraph ask: could this appear unchanged in someone else's repo? If yes, make it
   specific or delete it.
5. Check each claim against the code. If you can't verify it, remove it.
6. In `README.md` and [principles](../principles.md), replace any sentence Luis didn't say or approve with
   his wording from the interview.
