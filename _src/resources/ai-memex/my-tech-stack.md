---
title: My Technology Stack
description: A living reference documenting preferred technologies, tools, and rationale for technology choices.
entry_type: reference
published_date: "2026-04-01 00:00 -05:00"
last_updated_date: "2026-05-29 11:00 -05:00"
tags: meta, reference
source_project: lqdev-me
related_entries: codebase-context, current-focus
---

A living document describing the technologies and tools I reach for, and the reasoning behind those choices. Helps the AI Memex companion make recommendations that align with how I actually build — across all my projects, not just any one of them.

## Primary Languages

I'm a polyglot and pick the language to fit the job:

- **C# / .NET** — my workhorse for services, AI/ML, and experiments (.NET 10, ASP.NET Core, ML.NET, agent SDKs).
- **F#** — functional-first, type-safe; [lqdev.me](https://github.com/lqdev/luisquintanilla.me), the Webmention libraries, and slide tooling.
- **Rust** — systems and terminal tooling (e.g. [podcast-tui](https://github.com/lqdev/podcast-tui)); a growing focus.
- **Python** — scripting, data work, automation, and ML experiments.
- **JavaScript / TypeScript** — browser extensions, PWAs, progressive-enhancement widgets, and the [Mycelium MVP](https://github.com/lqdev/mycelium-mvp).
- **Emacs Lisp** — extending Emacs into an AI-native environment ([copilot-emacs](https://github.com/lqdev/copilot-emacs), [copilot-sdk-elisp](https://github.com/lqdev/copilot-sdk-elisp)).

## Preferred Frameworks & Libraries

- **.NET**: ASP.NET Core minimal APIs, ML.NET, .NET Aspire.
- **F#**: Giraffe ViewEngine (type-safe HTML, never string concatenation), Markdig (custom block extensions).
- **Data / storage**: DuckDB for embedded analytics.
- **Web**: Fuse.js for client-side search; vanilla JS progressive enhancement over heavy frameworks.
- **Protocols**: ActivityPub, AT Protocol, Matrix, Webmention, ListenBrainz.

## Development Environment

- **Editors**: **Emacs** (heavily — to the point of building Copilot-native tooling for it) and VS Code with Ionide for F#.
- **Authoring**: org-capture templates for hierarchical content capture.
- **OS / terminal**: Windows + PowerShell day to day; Linux via devcontainers / Codespaces.
- **AI tools**: GitHub Copilot deeply — not just using it, but building SDKs and agent integrations around it.

## Infrastructure & Hosting

- **Azure**: Static Web Apps, Functions, Blob Storage, Container Instances, Key Vault.
- **CI/CD**: GitHub Actions — builds, content automation, transcription, and scheduled workflows.
- **Self-hosting**: Matrix/Synapse, Owncast, my own scrobbler — owning the stack where it matters.
- **Decentralized protocols as infrastructure** — federation over platforms.

## Technology Principles

- **Own your data** — IndieWeb first. My site is the canonical home; platforms are syndication targets.
- **Open protocols over walled gardens** — interoperate, don't lock in.
- **AI-native** — treat AI agents and Copilot as first-class collaborators, and build the tooling to make that real.
- **Polyglot pragmatism** — the right language for the job, not one language for everything.
- **Self-host and decentralize** where it matters.
- **Static over dynamic** when possible; dynamic behavior arrives as progressive enhancement.
- **Type safety over runtime flexibility**; **privacy by aggregation** (vault stays private, garden is shared); **simple over clever**.
