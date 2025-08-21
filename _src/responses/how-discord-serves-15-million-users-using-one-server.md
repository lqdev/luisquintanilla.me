---
title: "How Discord Serves 15-Million Users on One Server"
targeturl: https://blog.bytebytego.com/p/how-discord-serves-15-million-users
response_type: reshare
dt_published: "2024-01-17 20:40 -05:00"
dt_updated: "2024-01-17 20:40 -05:00"
tags: ["beam","erlang","elixir","discord","scale","enterprise","chat","communities"]
---

> In early summer 2022, the Discord operations team noticed unusually high activity on their dashboards. They thought it was a bot attack, but it was legitimate traffic from MidJourney - a new, fast-growing community for generating AI images from text prompts.
> <br>
> To use MidJourney, you need a Discord account. Most MidJourney users join one main Discord server. This server grew so quickly that it soon hit Discord’s old limit of around 1 million users per server.
> <br>
> This is the story of how the Discord team creatively solved this challenge.

> Discord’s real-time messaging backend is built with Elixir. Elixir runs on the BEAM virtual machine. BEAM was created for Erlang - a language optimized for large real-time systems requiring rock-solid reliability and uptime.
> <br>
> A key capability BEAM provides is extremely lightweight parallel processes. This enables a single server to efficiently run tens or hundreds of thousands of processes concurrently.
> <br>
> Elixir brings friendlier, Ruby-inspired syntax to the battle-tested foundation of BEAM. Combined they make it much easier to program massively scalable, fault-tolerant systems.
> <br>
> So by leveraging BEAM's lightweight processes, the Elixir code powering Discord can "fan out" messages to hundreds of thousands of users around the world concurrently. However, limits emerge as communities grow larger.