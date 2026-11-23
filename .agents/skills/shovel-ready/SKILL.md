---
name: shovel-ready
description: Use when asked for a "shovel-ready" or "detailed" plan or issue, when deferring work to a follow-up issue, or when writing a task for someone else to carry out. Produces a plan or GitHub issue specific enough that a junior developer can finish it without asking a question.
---

# Write a shovel-ready plan or issue

The reader is a junior developer who has never seen this code and can't ask you anything. If they would have to guess, it isn't done.

## Steps

1. Verify before you write. Open every file you will name. Run every command you will give. If you couldn't run one, say so next to it.
2. Pick the template: `assets/issue.md` for one piece of work, `assets/plan.md` for work with several steps or pull requests.
3. Fill in every section. Write "None" where a section doesn't apply; don't delete it.
4. Check it against the list below.
5. For an issue: write the body to a file and create it with `gh issue create --title "{title}" --body-file {file}`. Create it only when the user asked for an issue or you are deferring work they approved.

## The bar

- Every file is named by path, and a line number where it helps.
- Every step starts with a verb and changes one thing.
- Every command can be copied and run, and the expected result is stated.
- "Done when" is a list of checks a person or a script can run, not a description.
- Decisions are made. If a choice is open, say who decides, and give your default.
- It says what is out of scope.
- It gives the evidence for the problem: an error message, a failing test, a measurement.
- It says what to do when a step fails.

## Gotchas

- Don't paste your investigation. Give the conclusion and the evidence a reader needs to trust it.
- Don't write "update the tests" or "handle errors". Name the test file and the case.
- Don't estimate time.
- A claim you didn't verify gets the words "not verified" beside it.
