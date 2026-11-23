---
applyTo: "ActivityPubBuilder.fs,Services/FollowersSync.fs,api/**,FEDERATION.md"
---

# ActivityPub

The build writes ActivityPub JSON, and the Azure Functions in `api/` serve it and handle the inbox.

- Generate activities at build time and treat them as immutable. The site sends no `Update` or `Delete`, so editing a post changes nothing on the Fediverse.
- Update `FEDERATION.md` in the same change whenever you change what federates. Other servers' developers read it, following FEP-67ff.
- Write `api/` as JavaScript for Node 22. Validate it in `api/` with `npm ci`, then `npm test`.
- Give a function everything it reads inside `api/`. Azure Static Web Apps deploys functions in isolation, so they can't read `_public/` or any sibling folder.
- Don't test against production. `Scripts/test-activitypub-production.sh` targets the live site, and the publish workflow delivers posts to real followers.

Read first: `docs/activitypub/README.md`, `FEDERATION.md`
