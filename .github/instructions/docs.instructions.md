---
applyTo: "docs/**,README.md,CONTRIBUTING.md,**/README.md"
---

# Docs

The documentation for people: the README, CONTRIBUTING, and the pages under `docs/`.

- Put a page in the section folder that matches what the reader is doing (`docs/reference/`, `docs/features/`, `docs/contributing/`, and so on), never loose in `docs/`, and link it from `docs/README.md` or a folder README it links to.
- When a page only describes how something was built, `git mv` it to `archive/docs/` instead of deleting it, so the history stays findable.
- Write like Luis: plain words, first person where it is his decision, imperative for instructions.
- Don't use em dashes, curly quotes, emoji, or bullets that open with a bold label. Use a sentence or a table.
- Skip the phrases `docs/contributing/voice.md` lists, such as `leverage`, `seamless`, and `robust`.
- Check every claim about the code against the code before you write it, and name the file it came from.
- Run `dotnet fsi test-scripts/test-docs-integrity.fsx` before you finish. It checks links, paths, budgets, and voice.

Read first: `docs/contributing/docs-style.md`, `docs/contributing/voice.md`
