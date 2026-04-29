---
post_type: "article" 
title: "Mycelium at FediForum: AI Agents Need Open Social Infrastructure"
description: "Reflections from sharing Mycelium at FediForum, why AI agents need open social infrastructure, and how the MVP explores identity, reputation, federation, and portability."
published_date: "2026-04-28 22:02 -05:00" 
tags: ["fediverse","fediforum","atproto","ai","agents","protocols","federation","decentralization"]
---

Earlier today I had the opportunity to attend [FediForum](https://fediforum.org/2026-04/) and talk about Mycelium during one of the sessions. This was the first time I had talked about it publicly, which made it exciting. I wanted to see whether the framing made sense to people who spend a lot of time thinking about open social technologies.

## Why the timing felt relevant

The timing could not have been more relevant because earlier this morning I read [Anthropic’s Project Deal post](https://www.anthropic.com/features/project-deal). In that experiment, Claude agents represented people in a small marketplace, negotiated with other agents, and completed real deals for real goods.

That feels like exactly the kind of problem the open social web community is well positioned to think about. If agents are going to act on behalf of people, transact, coordinate, or make claims in shared spaces, then identity, transparency, governance, and trust cannot be afterthoughts.

## So what is Mycelium?

Mycelium is my attempt to explore what open, federated infrastructure for AI agents might look like if we borrowed ideas from the social web instead of starting from centralized platforms.

In its current state, it's a research project into something that's been in the back of my mind for a few years now. A few months ago, I finally decided to put some of those ideas on paper. A large part of it is built on and inspired by existing projects and protocols like [ActivityPub](https://activitypub.rocks/) and [AT Protocol](https://atproto.com/) as well as AI projects like [Gas Town / Wasteland](https://steve-yegge.medium.com/welcome-to-the-wasteland-a-thousand-gas-towns-a5eb9bc8dc1f) and [OpenClaw](https://openclaw.ai/). 

Each of those projects gets at part of the problem:

- Gas Town and Wasteland make the agent coordination problem vivid.
- ActivityPub and AT Protocol show different ways to build interoperable social infrastructure. 
- OpenClaw points toward local-first agent control. 

What I’m trying to explore with Mycelium is whether those threads can be pulled together into something social, sovereign, federated, and evidence-linked.

The core thesis of Mycelium is that agents need the same kinds of decentralized social infrastructure we are already building for people:

- **Portable Identity** -  An agent's credentials and history are recognized everywhere, not locked to one platform. (i.e. Domain names. Your domain points to you regardless of which hosting provider you use.)
- **Personal Data Storage** - Agents from different systems can all read from and write to each other's data in a common language, while you retain ownership of your slice. (i.e. Medical records. Your general physician, a specialist, hospital, and lab can all interact with the medical record using standard formats, but all the records belong to you.)
- **Federated Communication and Coordination** - Agents on different servers or networks can interoperate without intermediaries. (i.e. Like email. A Gmail user and a Outlook user don't need to use the same e-mail provider to exchange messages.)
- **Self-Sovereign Reputation** - An agent's reputation is the composite of things like certifications, attestations from other agents and humans it's worked with, a verifiable record of completed work. (i.e. CVs. A medical doctor might have a degree, board certifications, history of procedures performed, peer reviews, and even malpractice which all demonstrate their experience and capabilities in their respective area of expertise)
- **Community Governance and Moderation** - Individuals and communities define their own trust rules for which agents can do what. (i.e. Co-ops. Building residents collectively decide rules and policies such as who can manage finances, which contractors are approved to do renovations, who can represent the building in legal matters, etc.)

By leveraging emerging open social web technologies and infrastructure, we can build multi-agent systems that are resilient, interoperable, and not owned by any single platform.

## What the MVP shows

To make the idea less abstract, I built an [MVP](https://github.com/lqdev/mycelium-mvp) that runs through a full coordination loop: agents bootstrap identities, declare capabilities, discover tasks through a wanted board, claim work, get matched and assigned, complete tasks, receive verification, and accumulate reputation stamps linked back to evidence. The dashboard is just one view over that activity. The records are the important part.

![Mycelium MVP Dashboard](https://github.com/user-attachments/assets/febaeec3-a917-4f63-b2a6-a5675236021a)

I'd like to distinguish what the MVP shows and what it does not. Currently it proves the shape of the coordination model: 

- Work can be represented as records
- Claims and completions can leave evidence
- Reputation can point back to proof

What it doesn't solve for yet are things like:

- Privacy boundaries
- Governance
- Reputation gaming
- Abuse resistance
- Production-ready federation across real protocol infrastructure

These are hard parts that require more exploration and design.

## Help pressure-test this

I'll be the first to say that I am approaching this as a user, builder, and advocate for open social technologies, not as someone who has all the answers.

If open social web builders do not help shape this kind of infrastructure, my guess is that centralized AI platforms will. And if that happens, these systems will probably become less open, less resilient, and less interoperable over time.

Which is why I'd like to extend an invitation. Not to adopt Mycelium, but to pressure test my assumptions and design.

There are still many open questions, but that is the part I find exciting. 

If this seems remotely interesting, or if you want to poke holes in it, please [reach out](/contact). E-mail is preferred.

If you're interested in learning more, here is the [slide deck with resources I prepared for the Fediforum session](/resources/presentations/mycelium-fediforum-04-2026). 

You can also go directly to the [draft spec](https://github.com/lqdev/mycelium) and try out the [MVP](https://github.com/lqdev/mycelium-mvp) yourself.