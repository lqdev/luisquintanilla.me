# ActivityPub docs

This folder documents how I built and run the ActivityPub side of lqdev.me: the Azure Functions in
`api/` that answer WebFinger, serve the actor and outbox, take Follow requests, and deliver new posts
to followers' inboxes. [`FEDERATION.md`](../../FEDERATION.md) at the repo root is the public statement
of what the site federates and which FEPs it supports. Start there if you only want to know how to
interact with the site from another server.

| File | Question it answers |
| --- | --- |
| [`ARCHITECTURE-OVERVIEW.md`](ARCHITECTURE-OVERVIEW.md) | How do the static site, the Functions, and Azure storage fit together? |
| [`implementation-status.md`](implementation-status.md) | Which phases are built, and which decisions were made along the way? |
| [`follower-management-architecture.md`](follower-management-architecture.md) | Why can't a static site manage followers on its own, and where do they live? |
| [`deployment-guide.md`](deployment-guide.md) | How do I set up the Azure resources and deploy the Functions? |
| [`keyvault-setup.md`](keyvault-setup.md) | How is the signing key stored in Azure Key Vault and read at delivery time? |
| [`notes-function-proxy.md`](notes-function-proxy.md) | Why do activity files go through a Function, and how does it fix their Content-Type? |
| [`free-tier-monitoring-guide.md`](free-tier-monitoring-guide.md) | What can I monitor with the Static Web Apps free tier, without Application Insights? |
| [`phase4a-testing-guide.md`](phase4a-testing-guide.md) | How do I test Follow, Accept, and follower storage end to end with a real Mastodon account? |

The endpoints are documented in [`../../api/ACTIVITYPUB.md`](../../api/ACTIVITYPUB.md), and the test
and maintenance scripts in [`../../Scripts/ACTIVITYPUB-SCRIPTS.md`](../../Scripts/ACTIVITYPUB-SCRIPTS.md).

Phase plans, research notes, and completion summaries from building this are in
[`../../archive/docs/activitypub/`](../../archive/docs/activitypub/).
