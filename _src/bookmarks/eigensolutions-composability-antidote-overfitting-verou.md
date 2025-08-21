---
title: "Eigensolutions: composability as the antidote to overfit "
targeturl: https://lea.verou.me/blog/2023/eigensolutions/
response_type: bookmark
dt_published: "2023-12-21 22:38 -05:00"
dt_updated: "2023-12-21 22:38 -05:00"
tags: ["design","product"]
---

> tl;dr: Overfitting happens when solutions don’t generalize sufficiently and is a hallmark of poor design. Eigensolutions are the opposite: solutions that generalize so much they expose links between seemingly unrelated use cases. Designing eigensolutions takes a mindset shift from linear design to composability.

> The eigensolution is a solution that addresses several key use cases, that previously appeared unrelated.

> ...it takes a mindset shift, from the linear Use case → Idea → Solution process to composability. Rather than designing a solution to address only our driving use cases, step back and ask yourself: can we design a solution as a composition of smaller, more general features, that could be used together to address a broader set of use cases?

> Contrary to what you may expect, eigensolutions can actually be quite hard to push to stakeholders:
>   
>   - Due to their generality, they often require significantly higher engineering effort to implement. Quick-wins are easier to sell: they ship faster and add value sooner. In my 11 years designing web technologies, I have seen many beautiful, elegant eigensolutions be vetoed due to implementation difficulties in favor of far more specific solutions — and often this was the right decision, it’s all about the cost-benefit.
>   - Eigensolutions tend to be lower level primitives, which are more flexible, but can also involve higher friction to use than a solution that is tailored to a specific use case.

> Eigensolutions tend to be lower level primitives. They enable a broad set of use cases, but may not be the most learnable or efficient way to implement all of them, compared to a tailored solution. In other words, they make complex things possible, but do not necessarily make common things easy.

> Instead of implementing tailored solutions ad-hoc (risking overfitting), they can be implemented as shortcuts: higher level abstractions using the lower level primitive. Done well, shortcuts provide dual benefit: not only do they reduce friction for common cases, they also serve as teaching aids for the underlying lower level feature. This offers a very smooth ease-of-use to power curve: if users need to go further than what the shortcut provides, they can always fall back on the lower level primitive to do so. 

> In an ideal world, lower level primitives and higher level abstractions would be designed and shipped together. However, engineering resources are typically limited, and it often makes sense to ship one before the other, so we can provide value sooner.

> This can happen in either direction:
>   
>    - Lower level primitive first. Shortcuts to make common cases easy can ship at a later stage, and demos and documentation to showcase common “recipes” can be used as a stopgap meanwhile. This prioritizes use case coverage over optimal UX, but it also allows collecting more data, which can inform the design of the shortcuts implemented.
>    - Higher level abstraction first, as an independent, ostensibly ad hoc feature. Then later, once the lower level primitive ships, it is used to “explain” the shortcut, and make it more powerful. This prioritizes optimal UX over use case coverage: we’re not covering all use cases, but for the ones we are covering, we’re offering a frictionless user experience.

> ...despite the name eigensolution, it’s still all about the use cases: eigensolutions just expose links between use cases that may have been hard to detect, but seem obvious in retrospect...Requiring all use cases to precede any design work can be unnecessarily restrictive, as frequently solving a problem improves our understanding of the problem.