---
applyTo: ".github/workflows/**,.github/actions/**,.github/ISSUE_TEMPLATE/**"
---

# Workflows and issue templates

GitHub Actions build, test, and deploy the site, and issue templates turn an issue into a post.

- Know which workflow you are touching. `validate-site-upgrade.yml` runs on pull requests and only tests. `publish-azure-static-web-apps.yml` runs on pushes to main, deploys, and writes to Bluesky and the Fediverse.
- Set up .NET with `global-json-file: global.json`. Pull request CI restores with `--locked-mode`, so a lock-file mismatch fails there before it reaches the deploy.
- Add a label in `sync-issue-labels.yml` and a branch in `process-content-issue.yml` for every new issue template, or the issue is never processed.
- Make slugs the same way the existing templates do, as `docs/reference/slugs.md` describes, so filenames stay predictable.
- Upload media through the `upload-to-cdn` composite action in `.github/actions/`, not a new upload step.
- Pass secrets through `env:` and never echo or print them, including in debug output.

Read first: `docs/reference/build.md`, `docs/reference/slugs.md`
