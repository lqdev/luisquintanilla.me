---
name: triage-review-comments
description: 'Use when asked to validate, address, or respond to pull request review comments, including Copilot code review comments, for example "validate the code review comments on #123" or "you know the drill". Checks each comment against the code, decides accept, reject, or defer with evidence, applies accepted fixes without regressions, files an issue for each deferral, and replies in every thread.'
---

# Triage pull request review comments

A review comment is a claim, not an instruction. Some are right. Some are wrong. Some are right but belong in a later change. Your job is to find out which, and show your evidence.

## Steps

1. Load every review thread and every comment before filtering resolved threads or deciding what to do. Replace `{owner}`, `{repo}`, and `{number}`. GitHub GraphQL connections are paginated; a first page is not a complete review.

   ```text
   gh api graphql -f query='query($owner:String!,$name:String!,$number:Int!,$after:String){repository(owner:$owner,name:$name){pullRequest(number:$number){reviewThreads(first:100,after:$after){nodes{id isResolved isOutdated path line comments(first:100){nodes{databaseId body author{login}} pageInfo{hasNextPage endCursor}}} pageInfo{hasNextPage endCursor}}}}}' -F owner={owner} -F name={repo} -F number={number}
   ```

   Start without `-F after=...`, then pass the returned `reviewThreads.pageInfo.endCursor` as `-F after={cursor}` and repeat until `hasNextPage` is false. Record every thread ID and cursor. For each thread whose comments report `hasNextPage: true`, load its remaining comments with this query, passing the thread ID and then each returned comments cursor until exhausted:

   ```text
   gh api graphql -f query='query($threadId:ID!,$after:String){node(id:$threadId){... on PullRequestReviewThread{id comments(first:100,after:$after){nodes{databaseId body author{login}} pageInfo{hasNextPage endCursor}}}}}' -F threadId={thread_id}
   ```

   Start without `-F after=...`; for later pages add `-F after={cursor}`. Do not filter resolved threads, decide, reply, or resolve any thread until all review-thread pages and every comments page have been loaded. Then skip threads where `isResolved` is true.
2. Record a baseline before changing anything: run the project's build and tests and note the result.
3. For each thread, test the claim. Open the file at that line and read enough around it. If the comment says there is a bug, try to show it with a failing test, a small script, or a traced input.
4. Decide.

   | Decision | When |
   | --- | --- |
   | Accept | The claim is true and the fix belongs in this pull request. |
   | Reject | The claim is false, or the suggestion makes the code worse. Say why, with evidence. |
   | Defer | The claim is true, but the fix is out of scope, risky, or large. |

5. Apply accepted fixes one at a time. After each, build and run the tests that cover it. If something that passed in step 2 now fails, fix it or revert before going on.
6. For each deferral, file an issue with the `shovel-ready` skill. Link the pull request and the thread.
7. Reply in every thread, then resolve it: the commit that fixed it, the evidence for rejecting it, or the issue number for a deferral. Use your client's review-thread tool if it has one. Otherwise:

   ```text
   gh api repos/{owner}/{repo}/pulls/{number}/comments/{first_comment_database_id}/replies -f body='...'
   gh api graphql -f query='mutation($id:ID!){resolveReviewThread(input:{threadId:$id}){thread{isResolved}}}' -F id={thread_id}
   ```

8. Run the full validation once more, commit, and push. Finish with the summary table.

## Gotchas

- Don't accept a comment because it sounds sure of itself. Automated reviewers see a diff, not the codebase, and are often wrong.
- Don't reject a comment to save work. If the claim is true, it is accept or defer.
- One accepted fix can break another. That is why step 8 validates everything again.
- Reply to the first comment in a thread. The API rejects replies to replies.
- An outdated thread still needs a decision. The code may have moved, not been fixed.
- Don't merge, close the pull request, or dismiss a review. Replying and resolving threads is the whole job.

## Summary format

```text
| # | File and line | Comment, in a few words | Decision | Evidence | Commit or issue |
```
