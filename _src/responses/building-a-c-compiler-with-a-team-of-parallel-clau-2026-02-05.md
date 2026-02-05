---
title: "Building a C compiler with a team of parallel Claudes"
targeturl: https://www.anthropic.com/engineering/building-c-compiler
response_type: reshare
dt_published: "2026-02-05 18:53 -05:00"
dt_updated: "2026-02-05 18:53 -05:00"
tags: ["ai","agents","anthropic","claude","swarm"]
---

> ...I tasked 16 agents with writing a Rust-based C compiler, from scratch, capable of compiling the Linux kernel. Over nearly 2,000 Claude Code sessions and $20,000 in API costs, the agent team produced a 100,000-line compiler that can build Linux 6.9 on x86, ARM, and RISC-V.

> The compiler is an interesting artifact on its own, but I focus here on what I learned about designing harnesses for long-running autonomous agent teams: how to write tests that keep agents on track without human oversight, how to structure work so multiple agents can make progress in parallel, and where this approach hits its ceiling.