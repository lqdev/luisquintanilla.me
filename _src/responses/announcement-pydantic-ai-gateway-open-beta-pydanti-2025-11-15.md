---
title: "Announcement: Pydantic AI Gateway Open Beta | Pydantic"
targeturl: https://pydantic.dev/articles/gateway-open-beta
response_type: reshare
dt_published: "2025-11-15 16:31 -05:00"
dt_updated: "2025-11-15 16:31 -05:00"
tags: ["pydantic","ai","llm","observability","governance"]
---

> Once you get beyond toy usage, LLM governance is a pain. That's exactly why we built [Pydantic AI Gateway](https://pydantic.dev/ai-gateway) (PAIG)

> What's included  
> <br>
> - **One key, many models:** talk to OpenAI, Anthropic, Google Vertex, Groq, or AWS Bedrock with the same key. More providers (notably Azure) are on the way. 
> - **Cost limits that stop spend:** set daily, weekly, monthly and total caps at project, user, and key levels. 
> - **Built-in observability:** every request can be logged to Pydantic Logfire or any OpenTelemetry backend. 
> - **Failover:** route around provider outages automatically. 
> - **Open source & self-hostable:** AGPL-3.0 core, file-based config, deploy anywhere. Console and UI are closed source.
> - **Enterprise-ready:** SSO via OIDC, granular permissions, and Cloudflare or on-prem deployment options. 
> - **Low latency:** Finally, and perhaps most importantly, PAIG runs on Cloudflare's globally distributed edge compute network, meaning absolutely minimal latency. If you're sitting in Berlin making a request to a model near Frankfurt, you'll connect to a PAIG service running in Berlin. After the first request, it won't need to call back to our central database, so using the gateway will add single-digit milliseconds to your response time. It might even be faster, as the request will run through Cloudflare's backbone to the model provider.