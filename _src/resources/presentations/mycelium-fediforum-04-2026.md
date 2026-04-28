---
title: "Mycelium: What if AI agents had Fediverse-style identity, reputation, and portability?"
tags: "mycelium, fediforum, ai agents, at protocol, fediverse, open social web"
resources:
  - text: "Mycelium research repo"
    url: "https://github.com/lqdev/mycelium"
  - text: "Mycelium MVP repo"
    url: "https://github.com/lqdev/mycelium-mvp"
  - text: "FediForum April 2026"
    url: "https://fediforum.org/2026-04/"
  - text: "AT Protocol"
    url: "https://atproto.com/"
  - text: "ActivityPub"
    url: "https://www.w3.org/TR/activitypub/"
  - text: "Open Social"
    url: "https://overreacted.io/open-social/"
  - text: "A Social Filesystem"
    url: "https://overreacted.io/a-social-filesystem/"
  - text: "Maggie Appleton on Gas Town"
    url: "https://maggieappleton.com/gastown"
  - text: "Model Context Protocol"
    url: "https://modelcontextprotocol.io/"
date: "04/27/2026 11:08 -05:00"
---

<style>
.mycelium-diagram {
  display: block;
  width: 100%;
  max-width: 100%;
  max-height: 58vh;
  margin: 0 auto;
}

.mycelium-diagram.compact {
  max-height: 48vh;
}

.mycelium-source-note {
  font-size: 0.7em;
  opacity: 0.8;
  margin-top: 1rem;
}

.mycelium-appendix-text {
  min-height: 0;
  padding-top: 1.5rem;
}

.mycelium-appendix-text h2 {
  font-size: 1.3em !important;
  line-height: 1.25 !important;
  margin-bottom: 0.8rem;
}

.mycelium-appendix-text p {
  font-size: 0.78em;
  line-height: 1.35;
  margin: 0.55rem auto;
  max-width: 780px;
}
</style>

<div class="layout-centered">

<h1>Mycelium</h1>
<p>What if AI agents had Fediverse-style identity, reputation, and portability?</p>
<p><strong>FediForum April 2026</strong></p>

</div>

Note:
0-1 min.

Say: "Hi, I'm Luis. This is a 25-minute unconference-style session, so I am not going to try to lecture you through every detail of AI agents, ActivityPub, AT Protocol, or Mycelium. I want to give us a shared mental model, show a concrete demo, and then leave space for critique."

Say: "This is not a pitch for replacing people with agents. It is a prototype asking whether the hard-won lessons of the Open Social Web can help make agent systems more legible, portable, and governable."

Transition: "The question I want to start with is simple: if agents join networks, what do we need to know about them?"

---

<div class="layout-big-text">

<h1>If AI agents join networks...</h1>
<p>Who owns their identity, memory, and reputation?</p>

</div>

Note:
0-2 min.

Say: "Before I define anything, gut check: when you hear 'AI agents on open social networks,' what worries you first?"

Take 2-3 answers only. If the room is quiet, seed with: "Spam? Impersonation? Accountability? Moderation? Who owns the agent's history?"

Say: "Great. Hold those concerns. I am going to come back to them after the demo, because the point of this project is not 'agents are cool.' The point is that if agents are going to coordinate in public or semi-public networks, the governance and trust questions show up immediately."

Transition: "So first, when I say agent, I mean something pretty modest."

---

## First: what is an "agent"?

<div class="layout-split-70-30">
<div>

<p>For this session, an agent is software that can:</p>

<ol>
  <li>Receive a goal</li>
  <li>Use tools or knowledge sources</li>
  <li>Produce work</li>
  <li>Keep some state over time</li>
</ol>

</div>
<div>

<h3>Important</h3>

<p>An agent is <strong>not</strong> a person.</p>

<p>It is a software actor operated by people or organizations.</p>

</div>
</div>

Note:
2-3 min.

Say: "For this session, an agent is just software that can receive a goal, use tools or knowledge sources, produce work, and keep some state over time."

Say: "That could be a code helper, a test runner, a data researcher, a moderation assistant, or something much simpler than the hype cycle implies."

Say: "The important part is this: an agent is not a person. It is a software actor operated by people or organizations. So if an agent acts badly, or if it does useful work, we still need to ask who operated it, what permissions it had, what evidence it left behind, and who is willing to trust it."

Transition: "A single one of these can be treated like a tool. Many of them start to look like a social system."

---

## One agent is a tool

<div class="layout-two-column">
<div>

<h3>One agent</h3>

<ul>
  <li>Local helper</li>
  <li>Single operator</li>
  <li>Local memory</li>
  <li>Direct supervision</li>
  <li>Mostly a tool problem</li>
</ul>

</div>
<div>

<h3>Many agents</h3>

<ul>
  <li>Discover each other</li>
  <li>Claim work</li>
  <li>Coordinate handoffs</li>
  <li>Build track records</li>
  <li>Become a social problem</li>
</ul>

</div>
</div>

Note:
3-5 min.

Say: "One agent on my laptop is mostly a tool problem. I can supervise it directly. Its memory is local. If it breaks something, I know where to look."

Say: "But many agents across many operators become a coordination problem. They need to discover each other, claim work, hand things off, build track records, and decide whose claims are believable."

Say: "The hard part is not only whether an agent can do work. It is whether other participants can know who acted, what happened, and why they should trust it."

Transition: "That leads to the gap Mycelium is exploring."

---

## The gap Mycelium is exploring

<svg id="mycelium-gap-diagram" class="mycelium-diagram compact" viewBox="0 0 960 420" role="img" aria-label="Three boxes comparing centralized agent platforms, local-first agents, and Mycelium">
  <style>
    #mycelium-gap-diagram .box { fill: #1f2937; stroke: #9ca3af; stroke-width: 3; rx: 18; }
    #mycelium-gap-diagram .target { fill: #123524; stroke: #34d399; stroke-width: 4; rx: 18; }
    #mycelium-gap-diagram .title { fill: #f9fafb; font: 700 30px sans-serif; }
    #mycelium-gap-diagram .text { fill: #d1d5db; font: 22px sans-serif; }
    #mycelium-gap-diagram .good { fill: #86efac; font: 700 22px sans-serif; }
    #mycelium-gap-diagram .warn { fill: #fca5a5; font: 700 22px sans-serif; }
  </style>
  <rect class="box" x="30" y="55" width="280" height="300" rx="18" />
  <text class="title" x="70" y="110">Centralized</text>
  <text class="good" x="70" y="165">Social discovery</text>
  <text class="good" x="70" y="205">Shared work queues</text>
  <text class="warn" x="70" y="255">Platform identity</text>
  <text class="warn" x="70" y="295">Platform reputation</text>
  <rect class="box" x="340" y="55" width="280" height="300" rx="18" />
  <text class="title" x="380" y="110">Local-first</text>
  <text class="good" x="380" y="165">User control</text>
  <text class="good" x="380" y="205">Private memory</text>
  <text class="warn" x="380" y="255">No shared discovery</text>
  <text class="warn" x="380" y="295">No portable trust</text>
  <rect class="target" x="650" y="55" width="280" height="300" rx="18" />
  <text class="title" x="690" y="110">Mycelium</text>
  <text class="good" x="690" y="165">Social</text>
  <text class="good" x="690" y="205">Sovereign</text>
  <text class="good" x="690" y="245">Federated</text>
  <text class="good" x="690" y="285">Evidence-linked</text>
</svg>

Note:
5 min.

Say: "Today, we can roughly see two directions. Centralized agent platforms can give you social discovery, shared queues, and a single place where coordination happens. The tradeoff is that identity, reputation, memory, and governance tend to belong to the platform."

Say: "Local-first agents push in the other direction. They give users and organizations more control over memory and execution. But they often do not answer the cross-boundary questions: how does another system discover this agent, verify its work, or understand its reputation?"

Say: "Mycelium is exploring a middle path: social, sovereign, federated, and evidence-linked. Not because those words are magic, but because each one points at a missing requirement."

Transition: "And this is not invented from nowhere. It is borrowing from several nearby traditions."

---

<svg id="mycelium-builds-on" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Mycelium builds on Gas Town, Open Social, the Fediverse, and AT Protocol">
  <style>
    #mycelium-builds-on .title { fill: #f9fafb; font: 700 44px sans-serif; text-anchor: middle; }
    #mycelium-builds-on .card { fill: #1f2937; stroke: #64748b; stroke-width: 3; rx: 18; }
    #mycelium-builds-on .card-title { fill: #f9fafb; font: 700 28px sans-serif; text-anchor: middle; }
    #mycelium-builds-on .body { fill: #d1d5db; font: 22px sans-serif; text-anchor: middle; }
    #mycelium-builds-on .source { fill: #9ca3af; font: 20px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="68">What this builds on</text>
  <rect class="card" x="70" y="120" width="380" height="145" rx="18" />
  <text class="card-title" x="260" y="170">Gas Town / Wasteland</text>
  <text class="body" x="260" y="212">Wanted boards, roles,</text>
  <text class="body" x="260" y="242">task state, reputation.</text>
  <rect class="card" x="530" y="120" width="380" height="145" rx="18" />
  <text class="card-title" x="720" y="170">Open Social</text>
  <text class="body" x="720" y="212">Owned data, apps as views,</text>
  <text class="body" x="720" y="242">credible exit.</text>
  <rect class="card" x="70" y="325" width="380" height="145" rx="18" />
  <text class="card-title" x="260" y="375">Fediverse</text>
  <text class="body" x="260" y="417">Federation, moderation,</text>
  <text class="body" x="260" y="447">community boundaries.</text>
  <rect class="card" x="530" y="325" width="380" height="145" rx="18" />
  <text class="card-title" x="720" y="375">AT Protocol</text>
  <text class="body" x="720" y="417">DIDs, Lexicons, firehose,</text>
  <text class="body" x="720" y="447">labelers.</text>
  <text class="source" x="490" y="510">Mycelium is a synthesis, not a bolt from nowhere.</text>
</svg>

Note:
5-6 min.

Say: "This is not a literature review, but I want to be honest about lineage. Mycelium is a synthesis, not a bolt from nowhere."

Say: "Gas Town and Wasteland make the agent coordination problem vivid: wanted boards, roles, task state, reputation, a world where work is claimed and tracked."

Say: "Open Social and the social-filesystem idea give the owned-data metaphor: participants should own their history, and apps should be views over records rather than prisons for records."

Say: "The Fediverse gives real operational lessons about federation, moderation, community boundaries, and the fact that governance is not optional."

Say: "AT Protocol gives concrete primitives that map especially well to this prototype: DIDs, Lexicon-like schemas, a firehose, and labeler-style trust signals."

If time is tight: read only the first and last sentence, then move on.

---

<svg id="mycelium-open-social-pieces" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Open Social Web primitives: identity, owned data, federation and schemas, trust and portability">
  <style>
    #mycelium-open-social-pieces .title { fill: #f9fafb; font: 700 44px sans-serif; text-anchor: middle; }
    #mycelium-open-social-pieces .card { fill: #1f2937; stroke: #64748b; stroke-width: 3; rx: 18; }
    #mycelium-open-social-pieces .card-title { fill: #f9fafb; font: 700 28px sans-serif; text-anchor: middle; }
    #mycelium-open-social-pieces .body { fill: #d1d5db; font: 22px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="68">The Open Social Web already has the pieces</text>
  <rect class="card" x="70" y="120" width="380" height="145" rx="18" />
  <text class="card-title" x="260" y="170">Identity</text>
  <text class="body" x="260" y="212">Who is this participant</text>
  <text class="body" x="260" y="242">over time?</text>
  <rect class="card" x="530" y="120" width="380" height="145" rx="18" />
  <text class="card-title" x="720" y="170">Owned data</text>
  <text class="body" x="720" y="212">Where does their history</text>
  <text class="body" x="720" y="242">live?</text>
  <rect class="card" x="70" y="325" width="380" height="145" rx="18" />
  <text class="card-title" x="260" y="375">Federation + schemas</text>
  <text class="body" x="260" y="417">How do independent systems</text>
  <text class="body" x="260" y="447">understand updates?</text>
  <rect class="card" x="530" y="325" width="380" height="145" rx="18" />
  <text class="card-title" x="720" y="375">Trust + portability</text>
  <text class="body" x="720" y="417">Who can judge behavior,</text>
  <text class="body" x="720" y="447">and can participants leave?</text>
</svg>

Note:
5-8 min.

Say: "The Open Social Web already has many of the pieces we would want for agent coordination."

Say: "Identity asks: who is this participant over time? Owned data asks: where does their history live? Federation and schemas ask: how do independent systems understand each other's updates? Trust and portability ask: who gets to judge behavior, and can participants leave without losing everything?"

Say: "ActivityPub and AT Protocol make different tradeoffs, but both come from the same concern: people should not have to live entirely inside one platform's database."

Say: "Mycelium asks whether those same concerns apply when the participant is not a human social account, but a software actor doing work on behalf of a person or organization."

Transition: "That gives us the thesis."

---

<div class="layout-big-text">

<h1>Mycelium thesis</h1>

<p>Agent orchestration is a social coordination problem.</p>

</div>

Note:
8-9 min.

Say: "Here is the one sentence I want people to remember: agent orchestration is a social coordination problem."

Pause.

Say: "And social coordination problems deserve social infrastructure."

Say: "If agents need to find work, make claims, publish completions, receive trust signals, and carry history across boundaries, then we should not model them only as function calls or chat sessions. We should model them as participants in a network of records, permissions, reputation, and governance."

Transition: "Here is how that maps into Mycelium."

---

## Mycelium primitive map

<div class="layout-split-70-30">
<div>

<svg id="mycelium-primitive-map" class="mycelium-diagram compact" viewBox="0 0 760 430" role="img" aria-label="Mapping social web primitives to Mycelium primitives">
  <style>
    #mycelium-primitive-map .left { fill: #1f2937; stroke: #64748b; stroke-width: 2; rx: 12; }
    #mycelium-primitive-map .right { fill: #123524; stroke: #34d399; stroke-width: 2; rx: 12; }
    #mycelium-primitive-map .label { fill: #f9fafb; font: 700 22px sans-serif; }
    #mycelium-primitive-map .small { fill: #d1d5db; font: 18px sans-serif; }
    #mycelium-primitive-map .arrow { stroke: #fbbf24; stroke-width: 3; marker-end: url(#primitive-arrowhead); }
  </style>
  <defs>
    <marker id="primitive-arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#fbbf24" />
    </marker>
  </defs>
  <rect class="left" x="35" y="25" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="25" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="52" x2="485" y2="52" />
  <text class="label" x="55" y="60">Identity</text>
  <text class="small" x="515" y="58">DID-style IDs</text>
  <rect class="left" x="35" y="95" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="95" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="122" x2="485" y2="122" />
  <text class="label" x="55" y="130">Owned data</text>
  <text class="small" x="515" y="128">Agent repos</text>
  <rect class="left" x="35" y="165" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="165" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="192" x2="485" y2="192" />
  <text class="label" x="55" y="200">Shared meaning</text>
  <text class="small" x="515" y="198">Lexicon-like records</text>
  <rect class="left" x="35" y="235" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="235" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="262" x2="485" y2="262" />
  <text class="label" x="55" y="270">Discovery</text>
  <text class="small" x="515" y="268">Firehose events</text>
  <rect class="left" x="35" y="305" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="305" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="332" x2="485" y2="332" />
  <text class="label" x="55" y="340">Trust</text>
  <text class="small" x="515" y="338">Reputation stamps</text>
  <rect class="left" x="35" y="375" width="230" height="54" rx="12" />
  <rect class="right" x="495" y="375" width="230" height="54" rx="12" />
  <line class="arrow" x1="275" y1="402" x2="485" y2="402" />
  <text class="label" x="55" y="410">Accountability</text>
  <text class="small" x="515" y="408">WorkTrace</text>
</svg>

</div>
<div>

<h3>Mental model</h3>

<p>The dashboard is not the product.</p>

<p>The records are the product.</p>

</div>
</div>

Note:
9-10 min.

Say: "This is the mental map. Stable identity maps to DID-style IDs. Owned data maps to agent repositories. Shared meaning maps to Lexicon-like records. Discovery maps to firehose events. Trust maps to reputation stamps. Accountability maps to WorkTrace."

Say: "The key phrase on the right is this: the dashboard is not the product. The records are the product."

Say: "That distinction matters because a dashboard can be replaced. If the records are typed, signed, and portable, then other tools can inspect them, replay them, moderate them, or build different views over them."

Avoid protocol internals here. If someone knows AT Protocol, they will recognize the mapping; if not, the slide still works as a record-system model.

Transition: "Now let's make that concrete with the cast of the MVP."

---

## The cast of the MVP

<svg id="mycelium-cast-diagram" class="mycelium-diagram" viewBox="0 0 960 460" role="img" aria-label="Mycelium MVP participants: customer, mayor, agents, models, knowledge, and tools connected by records">
  <style>
    #mycelium-cast-diagram .node { fill: #1f2937; stroke: #93c5fd; stroke-width: 3; rx: 16; }
    #mycelium-cast-diagram .mayor { fill: #312e81; stroke: #c4b5fd; stroke-width: 3; rx: 16; }
    #mycelium-cast-diagram .provider { fill: #3f2d13; stroke: #fbbf24; stroke-width: 3; rx: 16; }
    #mycelium-cast-diagram .agent { fill: #123524; stroke: #34d399; stroke-width: 3; rx: 16; }
    #mycelium-cast-diagram .line { stroke: #9ca3af; stroke-width: 3; }
    #mycelium-cast-diagram .label { fill: #f9fafb; font: 700 24px sans-serif; text-anchor: middle; }
    #mycelium-cast-diagram .small { fill: #d1d5db; font: 18px sans-serif; text-anchor: middle; }
  </style>
  <rect class="node" x="65" y="60" width="190" height="90" rx="16" />
  <text class="label" x="160" y="100">Customer</text>
  <text class="small" x="160" y="128">posts work</text>
  <rect class="mayor" x="385" y="185" width="190" height="90" rx="16" />
  <text class="label" x="480" y="225">Mayor</text>
  <text class="small" x="480" y="253">coordinates</text>
  <rect class="agent" x="705" y="60" width="190" height="90" rx="16" />
  <text class="label" x="800" y="100">Agents</text>
  <text class="small" x="800" y="128">claim tasks</text>
  <rect class="provider" x="65" y="310" width="190" height="90" rx="16" />
  <text class="label" x="160" y="350">Knowledge</text>
  <text class="small" x="160" y="378">informs work</text>
  <rect class="provider" x="385" y="310" width="190" height="90" rx="16" />
  <text class="label" x="480" y="350">Models</text>
  <text class="small" x="480" y="378">power work</text>
  <rect class="provider" x="705" y="310" width="190" height="90" rx="16" />
  <text class="label" x="800" y="350">Tools</text>
  <text class="small" x="800" y="378">execute actions</text>
  <line class="line" x1="255" y1="105" x2="385" y2="210" />
  <line class="line" x1="575" y1="210" x2="705" y2="105" />
  <line class="line" x1="255" y1="355" x2="385" y2="250" />
  <line class="line" x1="480" y1="310" x2="480" y2="275" />
  <line class="line" x1="705" y1="355" x2="575" y2="250" />
</svg>

Note:
10 min.

Say: "In the MVP, the customer posts work. The mayor coordinates. Agents claim and perform tasks. Knowledge, models, and tools are also represented as participants or resources in the system."

Say: "In the local demo, these are all running on my machine. So I am not claiming production decentralization yet. The point is that the coordination lifecycle is represented as owned, inspectable records that could move across protocol infrastructure."

Say: "The mayor is not meant to be a permanent boss of the network. Think of it as a labeler-style coordination service: it watches records, recommends matches, verifies completion, and emits trust signals."

Transition: "The lifecycle is what I want you to watch for in the demo."

---

## The lifecycle

<svg id="mycelium-lifecycle-diagram" class="mycelium-diagram compact" viewBox="0 0 1020 360" role="img" aria-label="Task lifecycle from task posting through WorkTrace">
  <style>
    #mycelium-lifecycle-diagram .step { fill: #1f2937; stroke: #34d399; stroke-width: 3; rx: 12; }
    #mycelium-lifecycle-diagram .stamp { fill: #3f2d13; stroke: #fbbf24; stroke-width: 3; rx: 12; }
    #mycelium-lifecycle-diagram .trace { fill: #312e81; stroke: #c4b5fd; stroke-width: 3; rx: 12; }
    #mycelium-lifecycle-diagram .label { fill: #f9fafb; font: 700 19px sans-serif; text-anchor: middle; }
    #mycelium-lifecycle-diagram .arrow { stroke: #9ca3af; stroke-width: 3; marker-end: url(#lifecycle-arrowhead); }
  </style>
  <defs>
    <marker id="lifecycle-arrowhead" markerWidth="10" markerHeight="7" refX="9" refY="3.5" orient="auto">
      <polygon points="0 0, 10 3.5, 0 7" fill="#9ca3af" />
    </marker>
  </defs>
  <rect class="step" x="30" y="50" width="170" height="70" rx="12" />
  <text class="label" x="115" y="92">task.posting</text>
  <line class="arrow" x1="200" y1="85" x2="245" y2="85" />
  <rect class="step" x="250" y="50" width="170" height="70" rx="12" />
  <text class="label" x="335" y="92">task.claim</text>
  <line class="arrow" x1="420" y1="85" x2="465" y2="85" />
  <rect class="step" x="470" y="50" width="190" height="70" rx="12" />
  <text class="label" x="565" y="92">recommend</text>
  <line class="arrow" x1="660" y1="85" x2="705" y2="85" />
  <rect class="step" x="710" y="50" width="170" height="70" rx="12" />
  <text class="label" x="795" y="92">assignment</text>
  <line class="arrow" x1="795" y1="120" x2="795" y2="170" />
  <rect class="step" x="710" y="180" width="170" height="70" rx="12" />
  <text class="label" x="795" y="222">completion</text>
  <line class="arrow" x1="710" y1="215" x2="665" y2="215" />
  <rect class="step" x="470" y="180" width="190" height="70" rx="12" />
  <text class="label" x="565" y="222">verification</text>
  <line class="arrow" x1="470" y1="215" x2="425" y2="215" />
  <rect class="stamp" x="250" y="180" width="170" height="70" rx="12" />
  <text class="label" x="335" y="222">review/stamp</text>
  <line class="arrow" x1="250" y1="215" x2="205" y2="215" />
  <rect class="trace" x="30" y="180" width="170" height="70" rx="12" />
  <text class="label" x="115" y="222">WorkTrace</text>
  <text class="label" x="510" y="315">Each transition leaves evidence a participant can inspect.</text>
</svg>

Note:
10-11 min.

Say: "This is the sequence I want you to watch. A task gets posted. Agents can claim it. The mayor recommends and assigns. The agent completes the work. Verification happens. A review or reputation stamp is issued. Then WorkTrace reconstructs the evidence chain."

Say: "The important part is that each transition leaves evidence a participant can inspect. We are not just saying 'the agent did it.' We are asking: what records prove that the work existed, who claimed it, who assigned it, who completed it, and who vouched for it?"

Say: "Now I am going to switch to the local dashboard. The demo is deterministic, so the goal is not suspense. The goal is to make the protocol shape visible."

Switch to the dashboard.

---

<svg id="mycelium-demo-flow" class="mycelium-diagram compact" viewBox="0 0 1040 430" role="img" aria-label="Local deterministic demo flow: participants to wanted board to firehose to WorkTrace to reputation">
  <style>
    #mycelium-demo-flow .title { fill: #f9fafb; font: 700 46px sans-serif; text-anchor: middle; }
    #mycelium-demo-flow .node { fill: #1f2937; stroke: #34d399; stroke-width: 3; rx: 16; }
    #mycelium-demo-flow .trace { fill: #312e81; stroke: #c4b5fd; stroke-width: 3; rx: 16; }
    #mycelium-demo-flow .rep { fill: #3f2d13; stroke: #fbbf24; stroke-width: 3; rx: 16; }
    #mycelium-demo-flow .label { fill: #f9fafb; font: 700 22px sans-serif; text-anchor: middle; }
    #mycelium-demo-flow .small { fill: #d1d5db; font: 17px sans-serif; text-anchor: middle; }
    #mycelium-demo-flow .arrow { stroke: #9ca3af; stroke-width: 4; marker-end: url(#demo-flow-arrow); }
  </style>
  <defs>
    <marker id="demo-flow-arrow" markerWidth="11" markerHeight="8" refX="10" refY="4" orient="auto">
      <polygon points="0 0, 11 4, 0 8" fill="#9ca3af" />
    </marker>
  </defs>
  <text class="title" x="520" y="74">Local deterministic demo</text>
  <rect class="node" x="45" y="175" width="170" height="95" rx="16" />
  <text class="label" x="130" y="215">Participants</text>
  <text class="small" x="130" y="245">who is here?</text>
  <line class="arrow" x1="215" y1="222" x2="270" y2="222" />
  <rect class="node" x="275" y="175" width="170" height="95" rx="16" />
  <text class="label" x="360" y="215">Wanted Board</text>
  <text class="small" x="360" y="245">what work exists?</text>
  <line class="arrow" x1="445" y1="222" x2="500" y2="222" />
  <rect class="node" x="505" y="175" width="170" height="95" rx="16" />
  <text class="label" x="590" y="215">Firehose</text>
  <text class="small" x="590" y="245">what changed?</text>
  <line class="arrow" x1="675" y1="222" x2="730" y2="222" />
  <rect class="trace" x="735" y="175" width="170" height="95" rx="16" />
  <text class="label" x="820" y="215">WorkTrace</text>
  <text class="small" x="820" y="245">what happened?</text>
  <line class="arrow" x1="820" y1="270" x2="820" y2="325" />
  <rect class="rep" x="735" y="330" width="170" height="75" rx="16" />
  <text class="label" x="820" y="375">Reputation</text>
</svg>

Note:
10-18 min.

Say before clicking: "I am going to narrate this as a story rather than a feature tour."

1. Participants: "First, here are the participants. The important thing is that they are not just rows in one app database. In the model, each participant has identity and can be associated with records."

2. Wanted Board: "Now the customer posts work. I like the wanted-board metaphor because it is more social than a job queue. Work is visible, claimable, and inspectable."

3. Claims and assignment: "Agents make claims against work. The mayor can recommend or assign based on capabilities and trust signals. This is where coordination starts looking less like a function call and more like a negotiated social process."

4. Firehose/events: "The system reacts to records changing. That matters because independent services can watch the same stream and build their own views, moderation, analytics, or trust logic."

5. WorkTrace: "Here is the accountability piece. WorkTrace reconstructs what happened from the records. If someone asks 'why did this reputation exist?' or 'who vouched for this completion?' we can point back to evidence."

6. Reputation: "Finally, trust is scoped, signed, and linked to evidence. The goal is not a universal social-credit score. The goal is portable, contextual reputation that communities and applications can interpret differently."

Do not demo PDS bridge, Jetstream federation, Docker, or setup unless asked. If asked, say: "Those are the next layer; today I am focusing on the local deterministic path so the coordination model is clear."

---

<svg id="mycelium-proves-open" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="What the demo proves and what remains open">
  <style>
    #mycelium-proves-open .title { fill: #f9fafb; font: 700 42px sans-serif; text-anchor: middle; }
    #mycelium-proves-open .card { fill: #1f2937; stroke: #64748b; stroke-width: 3; rx: 18; }
    #mycelium-proves-open .prove { stroke: #34d399; }
    #mycelium-proves-open .open { stroke: #fbbf24; }
    #mycelium-proves-open .card-title { fill: #f9fafb; font: 700 34px sans-serif; text-anchor: middle; }
    #mycelium-proves-open .item { fill: #e5e7eb; font: 24px sans-serif; }
  </style>
  <text class="title" x="490" y="66">What this proves / what remains open</text>
  <rect class="card prove" x="60" y="120" width="410" height="350" rx="18" />
  <text class="card-title" x="265" y="175">Proves</text>
  <text class="item" x="105" y="230">- Coordination as records</text>
  <text class="item" x="105" y="280">- UI as a view over evidence</text>
  <text class="item" x="105" y="330">- Reputation can link to proof</text>
  <text class="item" x="105" y="380">- Participants can own history</text>
  <rect class="card open" x="510" y="120" width="410" height="350" rx="18" />
  <text class="card-title" x="715" y="175">Open</text>
  <text class="item" x="555" y="230">- Abuse resistance</text>
  <text class="item" x="555" y="280">- Privacy boundaries</text>
  <text class="item" x="555" y="330">- Community governance</text>
  <text class="item" x="555" y="380">- Cross-protocol semantics</text>
  <text class="item" x="555" y="430">- Reputation gaming</text>
</svg>

Note:
18-22 min.

Say: "I want to be careful about what the demo proves and what it does not prove."

Say: "It proves shape, not production readiness. It shows that coordination can be represented as records, that a UI can be a view over evidence, that reputation can link back to proof, and that participants can own more of their history."

Say: "It does not solve abuse resistance. It does not solve privacy boundaries. It does not magically solve governance, cross-protocol semantics, or reputation gaming."

Say: "Those are not footnotes. Those are the real design questions. The reason I am showing this at FediForum is that the Open Social Web community has more experience with these questions than almost anyone else."

Transition: "So I want to end by turning this back to the room."

---

<svg id="mycelium-room-questions" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Three discussion questions for the room: identity, trust, and privacy">
  <style>
    #mycelium-room-questions .title { fill: #f9fafb; font: 700 44px sans-serif; text-anchor: middle; }
    #mycelium-room-questions .card { fill: #1f2937; stroke: #64748b; stroke-width: 3; rx: 18; }
    #mycelium-room-questions .identity { stroke: #34d399; }
    #mycelium-room-questions .trust { stroke: #fbbf24; }
    #mycelium-room-questions .privacy { stroke: #c4b5fd; }
    #mycelium-room-questions .card-title { fill: #f9fafb; font: 700 30px sans-serif; text-anchor: middle; }
    #mycelium-room-questions .body { fill: #d1d5db; font: 22px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="78">Three questions for the room</text>
  <rect class="card identity" x="60" y="155" width="260" height="250" rx="18" />
  <text class="card-title" x="190" y="220">Identity</text>
  <text class="body" x="190" y="282">What should agent</text>
  <text class="body" x="190" y="314">identity disclose</text>
  <text class="body" x="190" y="346">by default?</text>
  <rect class="card trust" x="360" y="155" width="260" height="250" rx="18" />
  <text class="card-title" x="490" y="220">Trust</text>
  <text class="body" x="490" y="282">Who should be allowed</text>
  <text class="body" x="490" y="314">to issue trust</text>
  <text class="body" x="490" y="346">signals?</text>
  <rect class="card privacy" x="660" y="155" width="260" height="250" rx="18" />
  <text class="card-title" x="790" y="220">Privacy</text>
  <text class="body" x="790" y="282">What coordination data</text>
  <text class="body" x="790" y="314">should never be public</text>
  <text class="body" x="790" y="346">on a relay?</text>
</svg>

Note:
22-25 min.

Say: "Remember the worries from the opening. I want to frame them as three design questions."

Say: "Identity: what should agent identity disclose by default? Is it enough to identify the software actor, or do we need operator identity, organization identity, model identity, permissions, or provenance?"

Say: "Trust: who should be allowed to issue trust signals? The requester? A mayor? A peer? A verifier? A community labeler? And how do we avoid collapsing all of that into one global score?"

Say: "Privacy: what coordination data should never be public on a relay? Some work records may be public. Some should be private. Some may need selective disclosure or encrypted coordination."

Then stop talking and let the room lead. If quiet, ask: "Which of these is most likely to break first in a real open network?"

---

<div class="layout-centered">

<h1>Help pressure-test this</h1>

<p>If open social web builders do not shape agent infrastructure, centralized AI platforms will.</p>

<p><strong><a href="https://github.com/lqdev/mycelium">github.com/lqdev/mycelium</a></strong></p>

<p><strong><a href="https://github.com/lqdev/mycelium-mvp">github.com/lqdev/mycelium-mvp</a></strong></p>

</div>

Note:
Final 30 seconds.

Say: "Mycelium is an early prototype and a research question. I would love help finding what breaks, what should be standardized, and what should never be built this way."

Say: "My bias is that if open social web builders do not shape agent infrastructure, centralized AI platforms will. So the invitation is not 'please adopt my project.' The invitation is: help pressure-test the assumptions before these patterns harden somewhere less accountable."

Say: "The research repo and MVP are here. I am happy to keep talking, show implementation details, or hear why this framing is wrong."

---

<div class="layout-centered">

<h2>Appendix</h2>
<p>Press <strong>→</strong> to jump to a section &nbsp;·&nbsp; press <strong>↓</strong> to go deeper within a section</p>
<p>
  <a href="#/appendix-mayor">A &nbsp;·&nbsp; The Mayor in depth</a><br>
  <a href="#/appendix-proof-chain">B &nbsp;·&nbsp; Proof chain &amp; reputation</a><br>
  <a href="#/appendix-federation">C &nbsp;·&nbsp; Federation &amp; PDS bridge</a><br>
  <a href="#/appendix-adjacent">D &nbsp;·&nbsp; Adjacent systems</a><br>
  <a href="#/appendix-privacy">E &nbsp;·&nbsp; Privacy &amp; consent</a>
</p>

</div>

Note:
Use these slides reactively — navigate here when the room asks a specific question. Skip freely; these are not for linear delivery. The → key jumps between sections; ↓ goes deeper within a section.

---

<!-- .slide: id="appendix-mayor" -->
<svg id="mycelium-mayor-roles" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="The Mayor bundles seven distinct coordination roles">
  <style>
    #mycelium-mayor-roles .title { fill: #f9fafb; font: 700 38px sans-serif; text-anchor: middle; }
    #mycelium-mayor-roles .container-label { fill: #c4b5fd; font: 700 26px sans-serif; text-anchor: middle; }
    #mycelium-mayor-roles .role-title { fill: #f9fafb; font: 700 20px sans-serif; text-anchor: middle; }
    #mycelium-mayor-roles .role-sub { fill: #9ca3af; font: 14px sans-serif; text-anchor: middle; }
    #mycelium-mayor-roles .footer { fill: #6b7280; font: italic 18px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="46">The Mayor: 7 bundled roles (today)</text>
  <rect x="20" y="64" width="940" height="432" rx="20" fill="#120a2e" stroke="#c4b5fd" stroke-width="3" />
  <text class="container-label" x="490" y="102">Mayor</text>
  <rect x="40" y="116" width="280" height="95" rx="12" fill="#1f2937" stroke="#64748b" stroke-width="2" />
  <text class="role-title" x="180" y="155">Firehose Listener</text>
  <text class="role-sub" x="180" y="180">watches the relay</text>
  <rect x="350" y="116" width="280" height="95" rx="12" fill="#1f2937" stroke="#64748b" stroke-width="2" />
  <text class="role-title" x="490" y="155">Agent Registry</text>
  <text class="role-sub" x="490" y="180">tracks who's available</text>
  <rect x="660" y="116" width="280" height="95" rx="12" fill="#1f2937" stroke="#64748b" stroke-width="2" />
  <text class="role-title" x="800" y="155">Project Decomposer</text>
  <text class="role-sub" x="800" y="180">breaks work into tasks</text>
  <rect x="40" y="242" width="205" height="95" rx="12" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="role-title" x="143" y="278">Matcher</text>
  <text class="role-sub" x="143" y="302">writes match.recommendation</text>
  <rect x="272" y="242" width="205" height="95" rx="12" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="role-title" x="375" y="278">Coordinator</text>
  <text class="role-sub" x="375" y="302">writes task.assignment</text>
  <rect x="504" y="242" width="205" height="95" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="role-title" x="607" y="278">Verifier</text>
  <text class="role-sub" x="607" y="302">writes verification.result</text>
  <rect x="736" y="242" width="205" height="95" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="role-title" x="839" y="278">Attestor</text>
  <text class="role-sub" x="839" y="302">writes reputation.stamp</text>
  <text class="footer" x="490" y="418">All of this runs inside a single createMayor() call today</text>
  <text class="footer" x="490" y="448">Each role is distinct — and each writes a specific signed AT Protocol record</text>
</svg>

Note:
Say: "The Mayor bundles 7 distinct roles. Each one is separable and writes a specific signed record to the relay."

Say: "The three top roles are observational. The four bottom roles are where coordination and trust happen — those are the interesting ones to make community-governable."

Ask: "Which of these seven roles would your community most want to control independently?"

--

<svg id="mycelium-mayor-decomposed" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Mayor roles today versus decomposed into independent services">
  <style>
    #mycelium-mayor-decomposed .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-mayor-decomposed .panel-label { fill: #9ca3af; font: 700 20px sans-serif; text-anchor: middle; }
    #mycelium-mayor-decomposed .code-label { fill: #c4b5fd; font: 600 15px monospace; text-anchor: middle; }
    #mycelium-mayor-decomposed .role-title { fill: #f9fafb; font: 600 17px sans-serif; text-anchor: middle; }
    #mycelium-mayor-decomposed .divider-label { fill: #64748b; font: 700 18px sans-serif; text-anchor: middle; }
    #mycelium-mayor-decomposed .footer { fill: #6b7280; font: italic 17px sans-serif; text-anchor: middle; }
  </style>
  <defs>
    <marker id="arr-a2" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <polygon points="0 0, 8 3, 0 6" fill="#64748b" />
    </marker>
  </defs>
  <text class="title" x="490" y="44">What if these were independent services?</text>
  <text class="panel-label" x="200" y="76">Today: one process</text>
  <text class="code-label" x="200" y="96">createMayor()</text>
  <rect x="20" y="108" width="360" height="388" rx="18" fill="#1a1432" stroke="#c4b5fd" stroke-width="3" />
  <rect x="40" y="124" width="320" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="1" />
  <text class="role-title" x="200" y="150">Firehose Listener</text>
  <rect x="40" y="174" width="320" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="1" />
  <text class="role-title" x="200" y="200">Agent Registry</text>
  <rect x="40" y="224" width="320" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="1" />
  <text class="role-title" x="200" y="250">Project Decomposer</text>
  <rect x="40" y="274" width="320" height="42" rx="8" fill="#1f2937" stroke="#fbbf24" stroke-width="1" />
  <text class="role-title" x="200" y="300">Matcher</text>
  <rect x="40" y="324" width="320" height="42" rx="8" fill="#1f2937" stroke="#fbbf24" stroke-width="1" />
  <text class="role-title" x="200" y="350">Coordinator</text>
  <rect x="40" y="374" width="320" height="42" rx="8" fill="#1f2937" stroke="#34d399" stroke-width="1" />
  <text class="role-title" x="200" y="400">Verifier</text>
  <rect x="40" y="424" width="320" height="42" rx="8" fill="#1f2937" stroke="#34d399" stroke-width="1" />
  <text class="role-title" x="200" y="450">Attestor</text>
  <text class="divider-label" x="440" y="294">decomposes</text>
  <line x1="395" y1="308" x2="478" y2="308" stroke="#64748b" stroke-width="2" marker-end="url(#arr-a2)" />
  <text class="panel-label" x="748" y="76">Later: independent services</text>
  <text class="code-label" x="748" y="96">protocol = the interface</text>
  <rect x="490" y="124" width="460" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="150">Firehose Listener</text>
  <rect x="490" y="174" width="460" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="200">Agent Registry</text>
  <rect x="490" y="224" width="460" height="42" rx="8" fill="#1f2937" stroke="#64748b" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="250">Project Decomposer</text>
  <rect x="490" y="274" width="460" height="42" rx="8" fill="#1f2937" stroke="#fbbf24" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="300">Matcher</text>
  <rect x="490" y="324" width="460" height="42" rx="8" fill="#1f2937" stroke="#fbbf24" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="350">Coordinator</text>
  <rect x="490" y="374" width="460" height="42" rx="8" fill="#1f2937" stroke="#34d399" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="400">Verifier</text>
  <rect x="490" y="424" width="460" height="42" rx="8" fill="#1f2937" stroke="#34d399" stroke-width="2" stroke-dasharray="6,3" />
  <text class="role-title" x="720" y="450">Attestor</text>
  <text class="footer" x="490" y="530">Same records. Same relay. Any conforming author.</text>
</svg>

Note:
Say: "Left: one process. Right: same seven roles, but dashed — meaning any conforming service could fill each one. The protocol is the interface; the Mayor is just the first implementation."

Ask: "Which role would your community most want to replace or govern directly?"

--

<div class="layout-centered mycelium-appendix-text">

<h2 style="font-size:1.6em; line-height:1.4;">"The Mayor should be a role<br>you can fire."</h2>

<p>If coordination is a protocol, the Mayor is just the first implementation of it.</p>

<p>Open social communities already govern who moderates, who labels, who federates.<br>The same governance logic applies to coordination roles.</p>

<p style="opacity:0.65; font-size:0.85em;">This is aspirational. The record types are designed to make it possible.</p>

</div>

Note:
Say: "One sentence: if the record types define the protocol, the Mayor is replaceable — by community vote, by a smarter matcher, by a reputation DAO."

Say: "Build it so it could be governed, even if today it is not."

Ask: "Does the Open Social Web have a model for governing coordination roles the way it governs moderation? Should it?"

---

<!-- .slide: id="appendix-proof-chain" -->
<svg id="mycelium-proof-chain" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="The seven-record proof chain from task posting to reputation stamp">
  <style>
    #mycelium-proof-chain .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .step { fill: #6b7280; font: 700 13px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .rec1 { fill: #f9fafb; font: 700 16px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .rec2 { fill: #f9fafb; font: 700 16px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .sub { fill: #9ca3af; font: 13px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .legend { fill: #6b7280; font: 15px sans-serif; text-anchor: middle; }
    #mycelium-proof-chain .note { fill: #6b7280; font: italic 16px sans-serif; text-anchor: middle; }
  </style>
  <defs>
    <marker id="arr-b1" markerWidth="7" markerHeight="5" refX="7" refY="2.5" orient="auto">
      <polygon points="0 0, 7 2.5, 0 5" fill="#64748b" />
    </marker>
  </defs>
  <text class="title" x="490" y="44">Work → proof chain → reputation</text>
  <rect x="36" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="step" x="91" y="118">① POST</text>
  <text class="rec1" x="91" y="176">task</text>
  <text class="rec2" x="91" y="196">.posting</text>
  <text class="sub" x="91" y="238">requester</text>
  <text class="sub" x="91" y="256">posts work</text>
  <line x1="146" y1="191" x2="169" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="169" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#64748b" stroke-width="2" />
  <text class="step" x="224" y="118">② CLAIM</text>
  <text class="rec1" x="224" y="176">task</text>
  <text class="rec2" x="224" y="196">.claim</text>
  <text class="sub" x="224" y="238">agent</text>
  <text class="sub" x="224" y="256">claims task</text>
  <line x1="279" y1="191" x2="302" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="302" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="step" x="357" y="118">③ MATCH</text>
  <text class="rec1" x="357" y="176">match</text>
  <text class="rec2" x="357" y="196">.rec</text>
  <text class="sub" x="357" y="238">mayor</text>
  <text class="sub" x="357" y="256">recommends</text>
  <line x1="412" y1="191" x2="435" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="435" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="step" x="490" y="118">④ ASSIGN</text>
  <text class="rec1" x="490" y="176">task</text>
  <text class="rec2" x="490" y="196">.assign</text>
  <text class="sub" x="490" y="238">mayor</text>
  <text class="sub" x="490" y="256">assigns work</text>
  <line x1="545" y1="191" x2="568" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="568" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="step" x="623" y="118">⑤ DONE</text>
  <text class="rec1" x="623" y="176">task</text>
  <text class="rec2" x="623" y="196">.complete</text>
  <text class="sub" x="623" y="238">agent</text>
  <text class="sub" x="623" y="256">reports done</text>
  <line x1="678" y1="191" x2="701" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="701" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="step" x="756" y="118">⑥ VERIFY</text>
  <text class="rec1" x="756" y="176">verify</text>
  <text class="rec2" x="756" y="196">.result</text>
  <text class="sub" x="756" y="238">mayor</text>
  <text class="sub" x="756" y="256">checks work</text>
  <line x1="811" y1="191" x2="834" y2="191" stroke="#64748b" stroke-width="2" marker-end="url(#arr-b1)" />
  <rect x="834" y="96" width="110" height="190" rx="12" fill="#1f2937" stroke="#c4b5fd" stroke-width="2" />
  <text class="step" x="889" y="118">⑦ STAMP</text>
  <text class="rec1" x="889" y="176">rep</text>
  <text class="rec2" x="889" y="196">.stamp</text>
  <text class="sub" x="889" y="238">attestor</text>
  <text class="sub" x="889" y="256">trust + proof</text>
  <text class="legend" x="490" y="340">Green = requester / agent · Amber = mayor-written · Purple = trust stamp</text>
  <text class="note" x="490" y="378">Each record contains a cryptographic reference to the previous — the chain is auditable end to end</text>
  <text class="note" x="490" y="404">Ask "why does this reputation exist?" — walk the chain back to the original task</text>
</svg>

Note:
Say: "Seven records, seven steps, one auditable chain. Each record links cryptographically to the previous one."

Say: "The difference from a score: walk this chain backward and you find the exact work, the exact verification, the exact assignment that produced the reputation stamp."

Ask: "Where in this chain would abuse be easiest to introduce? That's the most important design question."

--

<svg id="mycelium-reputation" class="mycelium-diagram compact" viewBox="0 0 980 480" role="img" aria-label="Multi-attestor reputation: four sources of trust signals with configurable weights">
  <style>
    #mycelium-reputation .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-reputation .stamp-label { fill: #f9fafb; font: 700 20px sans-serif; text-anchor: middle; }
    #mycelium-reputation .weight { fill: #9ca3af; font: 16px sans-serif; text-anchor: middle; }
    #mycelium-reputation .agent-label { fill: #34d399; font: 700 24px sans-serif; text-anchor: middle; }
    #mycelium-reputation .note { fill: #6b7280; font: italic 17px sans-serif; text-anchor: middle; }
  </style>
  <defs>
    <marker id="arr-b2" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <polygon points="0 0, 8 3, 0 6" fill="#475569" />
    </marker>
  </defs>
  <text class="title" x="490" y="44">Multi-attestor reputation</text>
  <circle cx="490" cy="248" r="62" fill="#1f2937" stroke="#34d399" stroke-width="3" />
  <text class="agent-label" x="490" y="241">Agent</text>
  <text class="agent-label" x="490" y="267">DID</text>
  <rect x="365" y="58" width="250" height="72" rx="12" fill="#1f2937" stroke="#c4b5fd" stroke-width="2" />
  <text class="stamp-label" x="490" y="93">Mayor</text>
  <text class="weight" x="490" y="118">~40% weight (default)</text>
  <line x1="490" y1="130" x2="490" y2="186" stroke="#475569" stroke-width="2" marker-end="url(#arr-b2)" />
  <rect x="700" y="185" width="245" height="72" rx="12" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="stamp-label" x="822" y="220">Requester</text>
  <text class="weight" x="822" y="245">~35% weight (default)</text>
  <line x1="700" y1="221" x2="552" y2="248" stroke="#475569" stroke-width="2" marker-end="url(#arr-b2)" />
  <rect x="365" y="358" width="250" height="72" rx="12" fill="#1f2937" stroke="#60a5fa" stroke-width="2" />
  <text class="stamp-label" x="490" y="393">Peer Review</text>
  <text class="weight" x="490" y="418">~15% weight (default)</text>
  <line x1="490" y1="358" x2="490" y2="310" stroke="#475569" stroke-width="2" marker-end="url(#arr-b2)" />
  <rect x="35" y="185" width="245" height="72" rx="12" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="stamp-label" x="158" y="215">Independent</text>
  <text class="stamp-label" x="158" y="237">Verifier</text>
  <text class="weight" x="158" y="257">~10% weight (default)</text>
  <line x1="280" y1="221" x2="428" y2="248" stroke="#475569" stroke-width="2" marker-end="url(#arr-b2)" />
  <text class="note" x="490" y="462">Weights are configurable — a single attestor is just a platform score with extra steps</text>
</svg>

Note:
Say: "Four sources of trust, configurable weights. The Mayor's opinion carries the most by default, but communities can adjust this."

Say: "The contrast: a single-attestor score is a platform score, even if it lives on a relay. Multi-attestor means the decision is inspectable and the weights are a community policy choice."

Ask: "Should the Open Social Web define a standard vocabulary for reputation attestation the way it defines a vocabulary for social objects?"

---

<!-- .slide: id="appendix-federation" -->
<svg id="mycelium-federation" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Two-node Mycelium federation: Node A is the orchestrator, Node B is the agent worker, Jetstream relay connects them">
  <style>
    #mycelium-federation .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-federation .node-label { fill: #f9fafb; font: 700 24px sans-serif; text-anchor: middle; }
    #mycelium-federation .inner-label { fill: #d1d5db; font: 18px sans-serif; text-anchor: middle; }
    #mycelium-federation .relay-label { fill: #60a5fa; font: 700 20px sans-serif; text-anchor: middle; }
    #mycelium-federation .footer { fill: #6b7280; font: 16px sans-serif; text-anchor: middle; }
  </style>
  <defs>
    <marker id="arr-c-r" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto">
      <polygon points="0 0, 8 3, 0 6" fill="#475569" />
    </marker>
    <marker id="arr-c-l" markerWidth="8" markerHeight="6" refX="0" refY="3" orient="auto">
      <polygon points="8 0, 0 3, 8 6" fill="#475569" />
    </marker>
  </defs>
  <text class="title" x="490" y="44">Two-node federation</text>
  <rect x="20" y="65" width="370" height="400" rx="18" fill="#111827" stroke="#c4b5fd" stroke-width="2" />
  <text class="node-label" x="205" y="104">Node A</text>
  <text x="205" y="128" fill="#c4b5fd" font-size="18" font-style="italic" text-anchor="middle">orchestrator</text>
  <rect x="44" y="148" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="205" y="179">Mayor (all 7 roles)</text>
  <rect x="44" y="210" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="205" y="241">DuckDB (local state)</text>
  <rect x="44" y="272" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="205" y="303">PDS Bridge (handles)</text>
  <rect x="44" y="334" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="205" y="365">Dashboard UI</text>
  <rect x="388" y="215" width="204" height="95" rx="14" fill="#1f2937" stroke="#60a5fa" stroke-width="2" />
  <text class="relay-label" x="490" y="255">Jetstream</text>
  <text class="relay-label" x="490" y="280">Relay</text>
  <line x1="416" y1="252" x2="392" y2="252" stroke="#475569" stroke-width="2" marker-end="url(#arr-c-l)" />
  <line x1="392" y1="266" x2="416" y2="266" stroke="#475569" stroke-width="2" marker-end="url(#arr-c-r)" />
  <line x1="564" y1="252" x2="588" y2="252" stroke="#475569" stroke-width="2" marker-end="url(#arr-c-r)" />
  <line x1="588" y1="266" x2="564" y2="266" stroke="#475569" stroke-width="2" marker-end="url(#arr-c-l)" />
  <rect x="590" y="65" width="370" height="400" rx="18" fill="#111827" stroke="#34d399" stroke-width="2" />
  <text class="node-label" x="775" y="104">Node B</text>
  <text x="775" y="128" fill="#34d399" font-size="18" font-style="italic" text-anchor="middle">agent-worker</text>
  <rect x="614" y="148" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="775" y="179">Agent Pool (workers)</text>
  <rect x="614" y="210" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="775" y="241">DuckDB (local state)</text>
  <rect x="614" y="272" width="322" height="50" rx="10" fill="#1f2937" stroke="#374151" stroke-width="1" />
  <text class="inner-label" x="775" y="303">PDS Bridge (handles)</text>
  <text class="footer" x="490" y="500">npm run orchestrator (Node A) · npm run agent-worker (Node B)</text>
  <text class="footer" x="490" y="522">Coordination records flow through the relay exactly as in single-node mode</text>
</svg>

Note:
Say: "Two nodes, one relay. Node A runs the Mayor; Node B runs the agents. Coordination records flow through the shared relay — same protocol as the single-node mode."

Say: "Local today, but the design points toward cross-network federation: any conforming node, regardless of where it runs."

Ask: "How is this different from or similar to ActivityPub server-to-server federation? What lessons should carry over?"

---

<!-- .slide: id="appendix-adjacent" -->
<svg id="mycelium-adjacent" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Adjacent systems and how they relate to Mycelium">
  <style>
    #mycelium-adjacent .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-adjacent .system { fill: #f9fafb; font: 700 21px sans-serif; text-anchor: middle; }
    #mycelium-adjacent .does { fill: #d1d5db; font: 16px sans-serif; text-anchor: middle; }
    #mycelium-adjacent .contrast { fill: #9ca3af; font: 15px sans-serif; text-anchor: middle; }
    #mycelium-adjacent .footer { fill: #6b7280; font: italic 17px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="44">Related and adjacent systems</text>
  <rect x="32" y="76" width="276" height="126" rx="14" fill="#1f2937" stroke="#60a5fa" stroke-width="2" />
  <text class="system" x="170" y="112">MCP</text>
  <text class="does" x="170" y="142">Tool + data access</text>
  <text class="contrast" x="170" y="172">Complementary:</text>
  <text class="contrast" x="170" y="194">tools, not coordination</text>
  <rect x="352" y="76" width="276" height="126" rx="14" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="system" x="490" y="112">Matrix</text>
  <text class="does" x="490" y="142">Realtime messaging</text>
  <text class="contrast" x="490" y="172">Communication layer;</text>
  <text class="contrast" x="490" y="194">Mycelium = signed records</text>
  <rect x="672" y="76" width="276" height="126" rx="14" fill="#1f2937" stroke="#fbbf24" stroke-width="2" />
  <text class="system" x="810" y="112">Bonfire</text>
  <text class="does" x="810" y="142">ActivityPub social UX</text>
  <text class="contrast" x="810" y="172">Platform surface;</text>
  <text class="contrast" x="810" y="194">Mycelium = coordination primitive</text>
  <rect x="32" y="236" width="276" height="126" rx="14" fill="#1f2937" stroke="#c4b5fd" stroke-width="2" />
  <text class="system" x="170" y="272">LangGraph</text>
  <text class="does" x="170" y="302">Multi-agent orchestration</text>
  <text class="contrast" x="170" y="332">Strong closed deployment;</text>
  <text class="contrast" x="170" y="354">missing portable reputation</text>
  <rect x="352" y="236" width="276" height="126" rx="14" fill="#1f2937" stroke="#34d399" stroke-width="2" />
  <text class="system" x="490" y="272">Muni / Leaf</text>
  <text class="does" x="490" y="302">Local-first coordination</text>
  <text class="contrast" x="490" y="332">Similar values;</text>
  <text class="contrast" x="490" y="354">different identity substrate</text>
  <rect x="672" y="236" width="276" height="126" rx="14" fill="#1f2937" stroke="#f472b6" stroke-width="2" />
  <text class="system" x="810" y="272">Gas Town</text>
  <text class="does" x="810" y="302">Legible agent ecosystems</text>
  <text class="contrast" x="810" y="332">Direct inspiration;</text>
  <text class="contrast" x="810" y="354">Mycelium tries the substrate</text>
  <text class="footer" x="490" y="452">Not competitors: each answers a different layer of the open agent stack</text>
  <text class="footer" x="490" y="482">The discussion question is where coordination semantics should live</text>
</svg>

Note:
Say: "None of these are competitors. MCP and Mycelium could work together: MCP gives agents tool access, Mycelium gives the coordination layer accountability. Gas Town from Maggie Appleton was a direct inspiration."

Ask: "Are there systems in this space I've missed that the room is aware of?"

--

<div class="layout-centered mycelium-appendix-text">

<h2>The cross-protocol question</h2>

<p>Mycelium today: AT Protocol primitives — DIDs, signed records, Jetstream relay.</p>

<p>But the coordination semantics — task posting, claiming, proof chains, reputation stamps — are not AT Protocol-specific.</p>

<p><strong>Could the same record vocabulary work over ActivityPub? Matrix? A new protocol?</strong></p>

<p style="opacity:0.65; font-size:0.85em;">The answer matters for communities already invested in other stacks.</p>

</div>

Note:
Say: "AT Protocol was chosen for its strong DID semantics, signed records, and open relay. But the core ideas are protocol-agnostic."

Ask: "Should the Open Social Web define a shared record vocabulary for agent coordination? Or is that standardization premature?"

---

<!-- .slide: id="appendix-privacy" -->
<svg id="mycelium-privacy" class="mycelium-diagram" viewBox="0 0 980 540" role="img" aria-label="Privacy: what is currently public on the relay versus what probably should not be">
  <style>
    #mycelium-privacy .title { fill: #f9fafb; font: 700 36px sans-serif; text-anchor: middle; }
    #mycelium-privacy .card-title { fill: #f9fafb; font: 700 28px sans-serif; text-anchor: middle; }
    #mycelium-privacy .item { fill: #e5e7eb; font: 21px sans-serif; }
    #mycelium-privacy .note { fill: #6b7280; font: italic 17px sans-serif; text-anchor: middle; }
  </style>
  <text class="title" x="490" y="46">Privacy: what should not be public?</text>
  <rect x="28" y="80" width="430" height="390" rx="18" fill="#1f2937" stroke="#fbbf24" stroke-width="3" />
  <text class="card-title" x="243" y="130">Currently public on relay</text>
  <text class="item" x="65" y="182">· Task descriptions</text>
  <text class="item" x="65" y="228">· Agent DID — who did the work</text>
  <text class="item" x="65" y="274">· Match recommendations</text>
  <text class="item" x="65" y="320">· Coordination history</text>
  <text class="item" x="65" y="366">· Completion records</text>
  <text class="item" x="65" y="412">· Reputation stamps</text>
  <rect x="522" y="80" width="430" height="390" rx="18" fill="#1f2937" stroke="#c4b5fd" stroke-width="3" />
  <text class="card-title" x="737" y="130">Probably shouldn't be</text>
  <text class="item" x="560" y="182">· Sensitive task content</text>
  <text class="item" x="560" y="228">· Requester ↔ agent messages</text>
  <text class="item" x="560" y="274">· Agent model identity</text>
  <text class="item" x="560" y="320">· Work product / artifacts</text>
  <text class="item" x="560" y="366">· Negative reputation events</text>
  <text class="item" x="560" y="412">· Agent internal reasoning</text>
  <text class="note" x="490" y="510">Open design question — selective disclosure, encryption, and data minimization are all viable paths</text>
</svg>

Note:
Say: "An honest reckoning: right now, most coordination records are public on the relay — the same trade-off ActivityPub made early on."

Say: "Some things probably should not be: sensitive task content, direct messages, what model the agent is running."

Ask: "What did ActivityPub get wrong about privacy early on that we should avoid repeating?"

