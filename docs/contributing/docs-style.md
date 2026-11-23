# Docs style

Where a page goes, and how to check it before you open a pull request. For how the prose should
sound, see [voice](voice.md).

| Folder | The question it answers |
| --- | --- |
| `docs/` root | Why the site exists, how it fits together, and how to run it. Only `README.md`, `principles.md`, `architecture.md`, and `getting-started.md` sit here. |
| `docs/adr/` | Why a decision was made. One page per decision. |
| `docs/activitypub/`, `docs/atproto/`, `docs/follow/`, `docs/websub/` | How one protocol integration works. |
| `docs/guides/publishing/` | How do I publish this kind of post? |
| `docs/guides/authoring/` | How do I write or set up this content? |
| `docs/guides/operations/` | How do I run or maintain this part of the site? |
| `docs/features/` | What does this feature do and where is its code? |
| `docs/reference/` | What are the exact keys, URLs, flags, or values? |
| `docs/contributing/` | How do I change the docs? |

Every page has to be linked from `docs/README.md`, or from a folder README it links to.

When a page only describes how something was built, `git mv` it to `archive/docs/`. The docs
describe the site as it is now.

## Check your changes

```powershell
dotnet fsi test-scripts/test-docs-integrity.fsx
```

It exits 1 if any check fails. Add `--all` to see every finding instead of the first 15 per check.

1. Links: every relative link points at a file that exists.
2. Paths: every file name or repo path in code formatting exists. CI runs on Linux, so case matters there even when it passes on Windows.
3. Docs root: no other Markdown files sit directly in `docs/`.
4. Orphans: every page under `docs/` is reachable from `docs/README.md` in two hops.
5. Budgets: files with a line budget stay under it.
6. Skills: each skill folder has a `SKILL.md` with a valid name and description.
7. Voice: pages in the voice scope have no em dashes, curly quotes, emoji, or phrases on the cut list.

If a path token is an example or a generated file that git doesn't track, add it to
`test-scripts/docs-integrity-allow.txt` with a comment saying why. Don't list a real file that is
missing. Fix the page instead.
