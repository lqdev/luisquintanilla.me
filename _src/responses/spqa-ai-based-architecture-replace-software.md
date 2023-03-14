---
title: "SPQA: The AI-based Architecture That’ll Replace Most Existing Software"
targeturl: https://danielmiessler.com/blog/spqa-ai-architecture-replace-existing-software/
response_type: bookmark
dt_published: "2023-03-14 10:52"
dt_updated: "2023-03-14 10:52 -05:00"
---

> AI-based applications will be completely different than those we have today. The new architecture will be a far more elegant, four-component structure based around GPTs: State, Policy, Questions, and Action.

> A security program using SPQA
> 
> 1. **Choose the base model** — You start with the latest and greatest overall GPT model from OpenAI, Google, Meta, McKinsey, or whoever. Lots of companies will have one. Let’s call it OpenAI’s GPT-6. It already knows so incredibly much about security, biotech, project management, scheduling, meetings, budgets, incident response, and audit preparedness that you might be able to survive with it alone. But you need more personalized context.
> 
> 2. **Train your custom model** — Then you train your custom model which is based on your own data, which will stack on top of GPT-6. This is all the stuff in the STATE section above. It’s your company’s telemetry and context. Logs. Docs. Finances. Chats. Emails. Meeting transcripts. Everything. It’s a small company and there are compression algorithms as part of the Custom Model Generation (CMG) product we use, so it’s a total of 312TB of data. You train your custom model on that.
> 
> 3. **Train your policy model** — Now you train another model that’s all about your company’s desires. The mission, the goals, your anti-goals, your challenges, your strategies. This is the guidance that comes from humans that we’re using to steer the ACTION part of the architecture. When we ask it to make stuff for us, and build out our plans, it’ll do so using the guardrails captured here in the POLICY.
> 
> 4. **Tell the system to take the following actions** — Now the models are combined. We have GPT-6, stacked with our STATE model, also stacked with our POLICY model, and together they know us better than we know ourselves.