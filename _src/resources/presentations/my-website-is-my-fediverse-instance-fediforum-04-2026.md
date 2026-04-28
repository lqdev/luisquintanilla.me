---
title: "My Website Is My Fediverse Instance"
tags: "indieweb, fediverse, activitypub, social web, static sites, personal websites"
resources:
  - text: "FOSDEM 2026 Social Web thoughts"
    url: "https://lqdev.me/posts/fosdem-2026-social-web-thoughts/"
  - text: "Website now natively posts to the Fediverse"
    url: "https://lqdev.me/notes/website-now-natively-posts-to-the-fediverse-2026-01-22/"
  - text: "Implement ActivityPub on a static site series by maho.dev"
    url: "https://lqdev.me/responses/implement-activitypub-static-site-series-maho/"
  - text: "POSSE to Mastodon using RSS and Azure Logic Apps"
    url: "https://lqdev.me/posts/rss-to-mastodon-posse-azure-logic-apps/"
  - text: "HTTP Signature Verification and Migration Planning"
    url: "https://lqdev.me/posts/activitypub-implementation-progress-2026-01-23/"
  - text: "Webmentions are back thanks to GitHub Copilot"
    url: "https://lqdev.me/posts/webmentions-are-back-2026-01/"
  - text: "ActivityPub"
    url: "https://www.w3.org/TR/activitypub/"
  - text: "IndieWeb POSSE"
    url: "https://indieweb.org/POSSE"
date: "04/27/2026 19:53 -05:00"
---

<style>
.fediverse-diagram {
  display: block;
  width: 100%;
  max-width: 100%;
  max-height: 58vh;
  margin: 0 auto;
}

.fediverse-diagram.compact {
  max-height: 48vh;
}

.fediverse-source-note {
  font-size: 0.68em;
  opacity: 0.78;
  margin-top: 0.8rem;
}

.fediverse-small-text {
  font-size: 0.78em;
  line-height: 1.35;
}

.fediverse-appendix-text {
  min-height: 0;
  padding-top: 1.5rem;
}

.fediverse-appendix-text h2 {
  font-size: 1.3em !important;
  line-height: 1.25 !important;
  margin-bottom: 0.8rem;
}

.fediverse-appendix-text p,
.fediverse-appendix-text li {
  font-size: 0.76em;
  line-height: 1.32;
}

.fediverse-appendix-text pre {
  font-size: 0.55em;
}
</style>

<div class="layout-centered">

<h1>My Website Is My Fediverse Instance</h1>
<p>Personal sites as social hubs, protocols as spokes</p>
<p><strong>Fediforum April 2026</strong></p>

</div>

Note:
0-1 min.

Say: "Hi, I'm Luis. This is a story about turning my personal website into a Fediverse-native publishing node without running a full Mastodon instance."

Say: "The bigger claim is not that everyone should hand-roll ActivityPub. The claim is that personal websites are already social infrastructure, and protocols can make them interoperable without asking us to move our identity somewhere else."

Transition: "I want to start with the least technical part: the web itself."

---

<div class="layout-big-text">

<h1>The web was already social</h1>
<p>People, links, feeds, replies, bookmarks, domains, archives.</p>

</div>

Note:
1-2 min.

Say: "In Social Web conversations, we sometimes talk like the social web is a separate thing from the web. I don't think that's right."

Say: "People use the web to learn, create, organize, argue, collaborate, share, and build community. That is already social."

Say: "The interesting question is: can we add richer social vocabulary to the web without replacing the web with another app-shaped silo?"

Transition: "That is why my starting point is not ActivityPub. It is the personal website."

---

## Start as a website

<div class="layout-two-column">
<div>

<h3>Already useful</h3>

<ul>
  <li>Permanent URLs</li>
  <li>Posts and notes</li>
  <li>RSS feeds</li>
  <li>Bookmarks and replies</li>
  <li>Searchable archive</li>
</ul>

</div>
<div>

<h3>Then add capabilities</h3>

<ul>
  <li>Webmentions</li>
  <li>Microformats</li>
  <li>ActivityPub</li>
  <li>Future protocol adapters</li>
  <li>Native client experiences</li>
</ul>

</div>
</div>

Note:
2-3 min.

Say: "The FOSDEM Social Web reflection that frames this talk is: start as a website, add social capabilities as components."

Say: "That makes the path more approachable. Your site still works as a site. It just gets progressively better at speaking social protocols."

Say: "This is also important for cost and maintenance. A static website is cheap and durable. The question becomes: what is the smallest dynamic layer needed to participate?"

Transition: "IndieWeb gives us a name for the publishing pattern."

---

## IndieWeb in one slide

<div class="layout-split-70-30">
<div>

<p><strong>Publish on your own site first.</strong></p>

<p>Use your domain as the stable home for your identity and content.</p>

<p>Syndicate elsewhere when useful, and receive responses back when possible.</p>

</div>
<div>

<h3>Vocabulary</h3>

<ul>
  <li>POSSE</li>
  <li>RSS</li>
  <li>Webmention</li>
  <li>Microformats</li>
</ul>

</div>
</div>

Note:
3-4 min.

Say: "POSSE is Post on your Own Site, Syndicate Elsewhere. The site is the canonical copy."

Say: "RSS lets people subscribe. Webmention lets sites notify each other. Microformats give posts machine-readable shape: author, content, reply target, like target, bookmark target."

Say: "That already looks like social infrastructure. It just does not always look like a modern social app."

Transition: "My first version of this was classic POSSE."

---

## One identity, many post types

<svg id="fediverse-post-types" class="fediverse-diagram compact" viewBox="0 0 960 420" role="img" aria-label="One website identity publishing many post types">
  <style>
    #fediverse-post-types .hub { fill: #123524; stroke: #34d399; stroke-width: 4; }
    #fediverse-post-types .node { fill: #1f2937; stroke: #9ca3af; stroke-width: 2.5; }
    #fediverse-post-types .title { fill: #f9fafb; font: 700 30px sans-serif; }
    #fediverse-post-types .text { fill: #d1d5db; font: 22px sans-serif; }
    #fediverse-post-types .line { stroke: #6ee7b7; stroke-width: 3; opacity: 0.75; }
  </style>
  <circle class="hub" cx="480" cy="210" r="82" />
  <text class="title" x="410" y="202">lqdev.me</text>
  <text class="text" x="416" y="235">identity</text>
  <line class="line" x1="480" y1="128" x2="480" y2="58" />
  <rect class="node" x="385" y="20" width="190" height="58" rx="16" />
  <text class="text" x="440" y="57">Articles</text>
  <line class="line" x1="557" y1="153" x2="710" y2="74" />
  <rect class="node" x="698" y="38" width="190" height="58" rx="16" />
  <text class="text" x="754" y="75">Notes</text>
  <line class="line" x1="575" y1="210" x2="762" y2="210" />
  <rect class="node" x="738" y="181" width="190" height="58" rx="16" />
  <text class="text" x="782" y="218">Responses</text>
  <line class="line" x1="557" y1="267" x2="710" y2="346" />
  <rect class="node" x="698" y="324" width="190" height="58" rx="16" />
  <text class="text" x="742" y="361">Bookmarks</text>
  <line class="line" x1="403" y1="267" x2="250" y2="346" />
  <rect class="node" x="72" y="324" width="190" height="58" rx="16" />
  <text class="text" x="120" y="361">Reviews</text>
  <line class="line" x1="385" y1="210" x2="198" y2="210" />
  <rect class="node" x="32" y="181" width="190" height="58" rx="16" />
  <text class="text" x="88" y="218">Media</text>
  <line class="line" x1="403" y1="153" x2="250" y2="74" />
  <rect class="node" x="72" y="38" width="190" height="58" rx="16" />
  <text class="text" x="122" y="75">RSVPs</text>
</svg>

Note:
4-5 min.

Say: "One of the big FOSDEM themes that resonated with me was one identity, many post types."

Say: "Instead of microblogging identity over here, photo identity over there, video identity somewhere else, review identity somewhere else: what if one account is where the content originates, and different clients consume the parts they understand?"

Say: "That is how I think about my site. Articles, notes, responses, bookmarks, media, reviews, RSVPs: all from one identity."

Transition: "Before ActivityPub, I was already pushing pieces of this into Mastodon."

---

## My first setup: POSSE to Mastodon

<svg id="fediverse-posse-flow" class="fediverse-diagram compact" viewBox="0 0 960 390" role="img" aria-label="Website RSS feed syndicating to Mastodon through Azure Logic Apps">
  <style>
    #fediverse-posse-flow .box { fill: #1f2937; stroke: #9ca3af; stroke-width: 3; rx: 18; }
    #fediverse-posse-flow .site { fill: #123524; stroke: #34d399; stroke-width: 4; rx: 18; }
    #fediverse-posse-flow .platform { fill: #312e81; stroke: #a5b4fc; stroke-width: 3; rx: 18; }
    #fediverse-posse-flow .title { fill: #f9fafb; font: 700 30px sans-serif; }
    #fediverse-posse-flow .text { fill: #d1d5db; font: 22px sans-serif; }
    #fediverse-posse-flow .arrow { stroke: #f9fafb; stroke-width: 4; marker-end: url(#arrow-posse); }
  </style>
  <defs><marker id="arrow-posse" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L9,3 z" fill="#f9fafb" /></marker></defs>
  <rect class="site" x="35" y="118" width="230" height="130" rx="18" />
  <text class="title" x="82" y="172">Website</text>
  <text class="text" x="82" y="212">canonical post</text>
  <line class="arrow" x1="270" y1="183" x2="382" y2="183" />
  <rect class="box" x="397" y="118" width="180" height="130" rx="18" />
  <text class="title" x="449" y="172">RSS</text>
  <text class="text" x="428" y="212">new item</text>
  <line class="arrow" x1="582" y1="183" x2="694" y2="183" />
  <rect class="box" x="705" y="80" width="220" height="90" rx="18" />
  <text class="title" x="754" y="135">Logic App</text>
  <line class="arrow" x1="815" y1="175" x2="815" y2="238" />
  <rect class="platform" x="705" y="246" width="220" height="90" rx="18" />
  <text class="title" x="758" y="301">Mastodon</text>
</svg>

<p class="fediverse-source-note">Original pattern: RSS feed triggers Azure Logic Apps, which POSTs to the Mastodon REST API.</p>

Note:
5-6 min.

Say: "The first version was simple and worked well. My website published the canonical post. RSS exposed updates. Azure Logic Apps watched the feed and posted to Mastodon."

Say: "This is a very practical bridge. It gave me reach without moving the canonical copy."

Transition: "But there was a catch: Mastodon was still the social runtime."

---

## What POSSE solved

<div class="layout-two-column">
<div>

<h3>Good</h3>

<ul>
  <li>Site stayed canonical</li>
  <li>RSS was simple</li>
  <li>Automation was cheap</li>
  <li>Followers saw updates</li>
  <li>No manual cross-posting</li>
</ul>

</div>
<div>

<h3>Not enough</h3>

<ul>
  <li>Mastodon was still the account</li>
  <li>The post was often just a derivative status</li>
  <li>Interactions stayed elsewhere</li>
  <li>I still maintained an instance</li>
  <li>Platform API shaped the experience</li>
</ul>

</div>
</div>

Note:
6-7 min.

Say: "POSSE preserved ownership of the canonical content, but it did not fully preserve ownership of the social identity or interaction surface."

Say: "The Mastodon instance still mattered. It was where the social account lived, where follows lived, where notifications lived, and where federation operations happened."

Transition: "And running that instance had a cost."

---

<div class="layout-big-text">

<h1>Hosting is hard after provisioning</h1>
<p>The first deploy is not the whole story.</p>

</div>

Note:
7-8 min.

Say: "This is one of the big FOSDEM Social Web points that stuck with me. Getting Mastodon running is not necessarily the hardest part. The harder part is everything after provisioning."

Say: "Disk fills up. Federation cache grows. Upgrades happen. Database migrations happen. Monitoring happens. Moderation and abuse handling can happen."

Say: "For a single-user instance, those costs are not amortized across a community. I still want to participate in the Fediverse, but I do not want to pay the full maintenance tax of running Mastodon just for myself."

Transition: "That led to a different question."

---

## The pivot

<div class="layout-centered">

<h2>If my needs were...</h2>

<p>Fediverse identity + followability + post delivery</p>

<h2>...could my website provide those directly?</h2>

</div>

Note:
8-9 min.

Say: "This was the pivot inspired by Maho's static-site ActivityPub series."

Say: "I looked at what I was actually using my Mastodon instance for: a Fediverse presence and a way to get website posts into followers' timelines."

Say: "If my site could expose discovery, an actor profile, an outbox, an inbox for follows, and signed delivery, then maybe I did not need a whole Mastodon instance for my personal use case."

Transition: "Before the architecture, here's the small amount of vocabulary needed."

---

## ActivityPub vocabulary

<div class="layout-two-column">
<div>

<h3>For humans</h3>

<ul>
  <li><strong>Account</strong>: followable identity</li>
  <li><strong>Profile</strong>: actor document</li>
  <li><strong>Post</strong>: object or activity</li>
  <li><strong>Timeline delivery</strong>: signed POST to inboxes</li>
</ul>

</div>
<div>

<h3>For the protocol</h3>

<ul>
  <li><strong>WebFinger</strong>: account discovery</li>
  <li><strong>Actor</strong>: identity + public key</li>
  <li><strong>Outbox</strong>: published activities</li>
  <li><strong>Inbox</strong>: received activities</li>
</ul>

</div>
</div>

Note:
9-10 min.

Say: "For newcomers: a Fediverse server has to answer a few questions. Who is this account? Where is the profile? What have they published? Where do I send a Follow? How do I know a message was really sent by that actor?"

Say: "WebFinger resolves the account name. Actor JSON describes the profile and public key. Outbox lists published activities. Inbox receives follows and other activities. HTTP signatures prove identity for federation."

Transition: "The first identity step was making my domain resolve."

---

## Domain identity

<svg id="fediverse-webfinger-flow" class="fediverse-diagram compact" viewBox="0 0 960 390" role="img" aria-label="Fediverse account discovery using WebFinger and ActivityPub actor">
  <style>
    #fediverse-webfinger-flow .box { fill: #1f2937; stroke: #9ca3af; stroke-width: 3; rx: 18; }
    #fediverse-webfinger-flow .highlight { fill: #123524; stroke: #34d399; stroke-width: 4; rx: 18; }
    #fediverse-webfinger-flow .title { fill: #f9fafb; font: 700 28px sans-serif; }
    #fediverse-webfinger-flow .text { fill: #d1d5db; font: 21px sans-serif; }
    #fediverse-webfinger-flow .arrow { stroke: #f9fafb; stroke-width: 4; marker-end: url(#arrow-webfinger); }
  </style>
  <defs><marker id="arrow-webfinger" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L9,3 z" fill="#f9fafb" /></marker></defs>
  <rect class="highlight" x="40" y="130" width="260" height="110" rx="18" />
  <text class="title" x="82" y="178">@lqdev@lqdev.me</text>
  <text class="text" x="118" y="212">search handle</text>
  <line class="arrow" x1="310" y1="185" x2="410" y2="185" />
  <rect class="box" x="425" y="130" width="210" height="110" rx="18" />
  <text class="title" x="466" y="178">WebFinger</text>
  <text class="text" x="450" y="212">account lookup</text>
  <line class="arrow" x1="645" y1="185" x2="745" y2="185" />
  <rect class="box" x="760" y="130" width="160" height="110" rx="18" />
  <text class="title" x="806" y="178">Actor</text>
  <text class="text" x="788" y="212">profile JSON</text>
</svg>

Note:
10-11 min.

Say: "The handle is not magic. A server looks up acct:lqdev@lqdev.me through WebFinger and gets back the ActivityPub actor URL."

Say: "This was a key mental shift for me. Instead of my website pointing to my Mastodon account, my domain could become the entry point for my Fediverse identity."

Transition: "Then the site could become more than a discovery alias."

---

## The site became the actor

<div class="layout-split-70-30">
<div>

<p>The key step was not cross-posting harder.</p>

<p>It was making the website itself speak enough ActivityPub to be followed.</p>

</div>
<div>

<h3>Enough means</h3>

<ul>
  <li>WebFinger</li>
  <li>Actor</li>
  <li>Outbox</li>
  <li>Inbox</li>
  <li>Signatures</li>
  <li>Delivery</li>
</ul>

</div>
</div>

Note:
11-12 min.

Say: "The website did not become Mastodon. It became a small, single-purpose Fediverse participant."

Say: "This distinction matters. Mastodon is a full social application. My site is a publishing hub that speaks enough protocol to be discoverable, followable, and deliver posts."

Transition: "The architecture follows from that narrower goal."

---

## Static-first, dynamic at the edges

<svg id="fediverse-architecture" class="fediverse-diagram" viewBox="0 0 960 500" role="img" aria-label="Static first ActivityPub architecture with dynamic protocol edges">
  <style>
    #fediverse-architecture .static { fill: #123524; stroke: #34d399; stroke-width: 4; rx: 18; }
    #fediverse-architecture .dynamic { fill: #3b1f47; stroke: #d8b4fe; stroke-width: 3; rx: 18; }
    #fediverse-architecture .store { fill: #1e3a5f; stroke: #93c5fd; stroke-width: 3; rx: 18; }
    #fediverse-architecture .fed { fill: #312e81; stroke: #a5b4fc; stroke-width: 3; rx: 18; }
    #fediverse-architecture .title { fill: #f9fafb; font: 700 25px sans-serif; }
    #fediverse-architecture .text { fill: #d1d5db; font: 18px sans-serif; }
    #fediverse-architecture .arrow { stroke: #f9fafb; stroke-width: 3.5; marker-end: url(#arrow-arch); }
  </style>
  <defs><marker id="arrow-arch" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L9,3 z" fill="#f9fafb" /></marker></defs>
  <rect class="static" x="35" y="52" width="210" height="95" rx="18" />
  <text class="title" x="78" y="91">Markdown</text>
  <text class="text" x="76" y="120">site content</text>
  <line class="arrow" x1="250" y1="99" x2="354" y2="99" />
  <rect class="static" x="365" y="52" width="210" height="95" rx="18" />
  <text class="title" x="413" y="91">F# build</text>
  <text class="text" x="395" y="120">HTML, RSS, AP JSON</text>
  <line class="arrow" x1="580" y1="99" x2="684" y2="99" />
  <rect class="static" x="695" y="52" width="230" height="95" rx="18" />
  <text class="title" x="750" y="91">Static CDN</text>
  <text class="text" x="731" y="120">cheap, cacheable reads</text>
  <rect class="dynamic" x="70" y="242" width="230" height="130" rx="18" />
  <text class="title" x="116" y="285">Functions</text>
  <text class="text" x="99" y="318">WebFinger, actor,</text>
  <text class="text" x="101" y="344">inbox, outbox proxy</text>
  <rect class="store" x="365" y="242" width="230" height="130" rx="18" />
  <text class="title" x="416" y="285">State</text>
  <text class="text" x="407" y="318">Table + Queue</text>
  <text class="text" x="411" y="344">followers, delivery</text>
  <rect class="store" x="660" y="242" width="230" height="130" rx="18" />
  <text class="title" x="715" y="285">Key Vault</text>
  <text class="text" x="708" y="318">signing keys</text>
  <text class="text" x="688" y="344">not in source code</text>
  <line class="arrow" x1="810" y1="150" x2="810" y2="236" />
  <line class="arrow" x1="305" y1="307" x2="360" y2="307" />
  <line class="arrow" x1="600" y1="307" x2="655" y2="307" />
  <rect class="fed" x="365" y="410" width="230" height="65" rx="18" />
  <text class="title" x="418" y="452">Fediverse</text>
  <line class="arrow" x1="185" y1="375" x2="412" y2="406" />
  <line class="arrow" x1="775" y1="375" x2="548" y2="406" />
</svg>

Note:
12-14 min.

Say: "The core architecture is static-first. Markdown goes into the F# build. The build emits HTML, RSS, ActivityPub activities, activity files, and outbox pages."

Say: "Most reads are static files behind Azure Static Web Apps and CDN. The dynamic layer exists only where the protocol requires it: POST inboxes, signatures, correct ActivityPub content types, delivery queues, and persistent follower state."

Say: "That keeps the architecture closer to a static site than to a continuously running social server."

Transition: "Here's what happens when I publish."

---

## Publish flow

<svg id="fediverse-delivery-flow" class="fediverse-diagram" viewBox="0 0 960 500" role="img" aria-label="Content publishing flow from markdown to follower inboxes">
  <style>
    #fediverse-delivery-flow .box { fill: #1f2937; stroke: #9ca3af; stroke-width: 3; rx: 18; }
    #fediverse-delivery-flow .site { fill: #123524; stroke: #34d399; stroke-width: 4; rx: 18; }
    #fediverse-delivery-flow .queue { fill: #1e3a5f; stroke: #93c5fd; stroke-width: 3; rx: 18; }
    #fediverse-delivery-flow .fed { fill: #312e81; stroke: #a5b4fc; stroke-width: 3; rx: 18; }
    #fediverse-delivery-flow .title { fill: #f9fafb; font: 700 24px sans-serif; }
    #fediverse-delivery-flow .text { fill: #d1d5db; font: 18px sans-serif; }
    #fediverse-delivery-flow .arrow { stroke: #f9fafb; stroke-width: 3.5; marker-end: url(#arrow-delivery); }
  </style>
  <defs><marker id="arrow-delivery" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto"><path d="M0,0 L0,6 L9,3 z" fill="#f9fafb" /></marker></defs>
  <rect class="site" x="30" y="55" width="185" height="82" rx="18" />
  <text class="title" x="75" y="91">Write</text>
  <text class="text" x="67" y="118">markdown</text>
  <line class="arrow" x1="220" y1="96" x2="294" y2="96" />
  <rect class="box" x="305" y="55" width="185" height="82" rx="18" />
  <text class="title" x="350" y="91">Build</text>
  <text class="text" x="334" y="118">F# generator</text>
  <line class="arrow" x1="495" y1="96" x2="569" y2="96" />
  <rect class="box" x="580" y="55" width="185" height="82" rx="18" />
  <text class="title" x="625" y="91">Deploy</text>
  <text class="text" x="612" y="118">Static Web Apps</text>
  <line class="arrow" x1="673" y1="142" x2="673" y2="204" />
  <rect class="queue" x="555" y="215" width="235" height="92" rx="18" />
  <text class="title" x="607" y="252">Queue recent</text>
  <text class="text" x="595" y="280">Create activities</text>
  <line class="arrow" x1="552" y1="261" x2="430" y2="261" />
  <rect class="queue" x="210" y="215" width="215" height="92" rx="18" />
  <text class="title" x="249" y="252">Sign + POST</text>
  <text class="text" x="247" y="280">to follower inboxes</text>
  <line class="arrow" x1="318" y1="312" x2="318" y2="374" />
  <rect class="fed" x="198" y="385" width="240" height="82" rx="18" />
  <text class="title" x="260" y="422">Timeline</text>
  <text class="text" x="231" y="449">native Fediverse post</text>
  <line class="arrow" x1="398" y1="140" x2="398" y2="206" />
  <rect class="site" x="92" y="215" width="90" height="92" rx="18" />
  <text class="title" x="111" y="252">JSON</text>
  <text class="text" x="112" y="280">static</text>
</svg>

Note:
14-15 min.

Say: "On publish, the build generates the website and ActivityPub data. GitHub Actions deploys the site and queues recent Create activities."

Say: "A delivery worker signs those activities and POSTs them to follower inboxes. Shared inbox optimization means a server with many followers can get one delivery instead of one per follower."

Say: "This is not a long-running Mastodon replacement. It is a publish-and-deliver pipeline."

Transition: "The important bit is that the generated activities preserve intent."

---

## Native syndication

<div class="layout-two-column">
<div>

<h3>IndieWeb intent</h3>

<ul>
  <li>note</li>
  <li>article</li>
  <li>reply</li>
  <li>reshare</li>
  <li>star</li>
  <li>bookmark</li>
  <li>media</li>
  <li>review</li>
</ul>

</div>
<div>

<h3>Fediverse shape</h3>

<ul>
  <li><code>Note</code></li>
  <li><code>Article</code></li>
  <li><code>inReplyTo</code></li>
  <li><code>Announce</code></li>
  <li><code>Like</code></li>
  <li><code>Link</code> attachment</li>
  <li>media attachment</li>
  <li>review metadata</li>
</ul>

</div>
</div>

Note:
15-16 min.

Say: "Native syndication means more than copying text. If a post is a reply, it should be a reply. If it is a reshare, it should be an Announce. If it is a star, it should be a Like."

Say: "The site remains the source of truth, but the target platform should receive something that feels native to that platform."

Say: "This is where the IndieWeb data model and ActivityStreams vocabulary start to reinforce each other."

Transition: "Here is the actual demo path I would use."

---

## Demo path

<div class="layout-code-split">
<div>

<h3>Show the source</h3>

```yaml
title: "..."
response_type: reshare
targeturl: https://...
tags: ["fediverse","indieweb"]
```

<p class="fediverse-small-text">Markdown is still the authoring source.</p>

</div>
<div>

<h3>Show the protocol</h3>

```json
{
  "type": "Announce",
  "actor": "https://lqdev.me/api/activitypub/actor",
  "object": "https://..."
}
```

<p class="fediverse-small-text">ActivityPub JSON is generated from site semantics.</p>

</div>
</div>

Note:
16-18 min.

Say: "For the demo, I would keep it short and concrete."

Say: "First show the actual page on lqdev.me. Then show the markdown frontmatter. Then show the generated JSON. Then show WebFinger, actor, and outbox endpoints."

Say: "The point is not to teach the whole ActivityPub spec. The point is to show that the site has enough protocol surface to be legible to Fediverse software."

Transition: "And the cost profile is the reason this is viable for a personal site."

---

## Low-cost operating model

<div class="layout-two-column">
<div>

<h3>Move work to build time</h3>

<ul>
  <li>Generate HTML</li>
  <li>Generate RSS</li>
  <li>Generate activities</li>
  <li>Generate outbox pages</li>
  <li>Cache aggressively</li>
</ul>

</div>
<div>

<h3>Use runtime only for edges</h3>

<ul>
  <li>Follow requests</li>
  <li>HTTP signatures</li>
  <li>Delivery fan-out</li>
  <li>Follower state</li>
  <li>Correct AP headers</li>
</ul>

</div>
</div>

Note:
18-19 min.

Say: "The cost model is: do expensive or repeatable work once at build time, then serve static files whenever possible."

Say: "Azure Functions only handle protocol edges. Table Storage stores followers and delivery state. Queue Storage handles fan-out. Key Vault stores signing keys."

Say: "For my usage pattern, this is cents per month instead of paying for a continuously running single-user Mastodon instance."

Transition: "This is working, but it is intentionally not a complete social app."

---

## What is still missing?

<div class="layout-two-column">
<div>

<h3>Product gaps</h3>

<ul>
  <li>Reply inbox UX</li>
  <li>Notifications</li>
  <li>DMs</li>
  <li>Better migration story</li>
  <li>Conversation views</li>
</ul>

</div>
<div>

<h3>Protocol/community gaps</h3>

<ul>
  <li>Client rendering differences</li>
  <li>Moderation ergonomics</li>
  <li>Abuse handling</li>
  <li>Multi-protocol drift</li>
  <li>Consent boundaries</li>
</ul>

</div>
</div>

Note:
19-21 min.

Say: "The current model is mostly publishing-oriented. People can follow and receive posts, but I still need better ways to receive replies, notifications, and other interactions back into my site."

Say: "Webmentions are one possible bridge back. ActivityPub replies could become Webmentions or an owned notification feed. That is still design work."

Say: "Moderation and abuse are also real. A personal node is smaller than a community server, but it is still participating in public networks."

Transition: "The broader model is hub and spokes."

---

## Hub and spokes

<svg id="fediverse-hub-spokes" class="fediverse-diagram compact" viewBox="0 0 960 440" role="img" aria-label="Personal website as hub connected to multiple social protocols as spokes">
  <style>
    #fediverse-hub-spokes .hub { fill: #123524; stroke: #34d399; stroke-width: 4; }
    #fediverse-hub-spokes .spoke { fill: #1f2937; stroke: #9ca3af; stroke-width: 3; }
    #fediverse-hub-spokes .future { fill: #3b1f47; stroke: #d8b4fe; stroke-width: 3; }
    #fediverse-hub-spokes .title { fill: #f9fafb; font: 700 30px sans-serif; }
    #fediverse-hub-spokes .text { fill: #d1d5db; font: 22px sans-serif; }
    #fediverse-hub-spokes .line { stroke: #6ee7b7; stroke-width: 4; opacity: 0.8; }
  </style>
  <circle class="hub" cx="480" cy="220" r="88" />
  <text class="title" x="405" y="212">lqdev.me</text>
  <text class="text" x="420" y="246">source of truth</text>
  <line class="line" x1="480" y1="132" x2="480" y2="62" />
  <circle class="spoke" cx="480" cy="48" r="44" />
  <text class="text" x="458" y="56">RSS</text>
  <line class="line" x1="556" y1="175" x2="720" y2="82" />
  <circle class="spoke" cx="755" cy="62" r="60" />
  <text class="text" x="708" y="70">ActivityPub</text>
  <line class="line" x1="568" y1="220" x2="758" y2="220" />
  <circle class="future" cx="820" cy="220" r="60" />
  <text class="text" x="763" y="228">AT Protocol</text>
  <line class="line" x1="556" y1="265" x2="720" y2="358" />
  <circle class="future" cx="755" cy="378" r="50" />
  <text class="text" x="726" y="386">Nostr</text>
  <line class="line" x1="404" y1="265" x2="240" y2="358" />
  <circle class="spoke" cx="205" cy="378" r="64" />
  <text class="text" x="147" y="386">Webmention</text>
  <line class="line" x1="392" y1="220" x2="202" y2="220" />
  <circle class="spoke" cx="140" cy="220" r="52" />
  <text class="text" x="107" y="228">HTML</text>
  <line class="line" x1="404" y1="175" x2="240" y2="82" />
  <circle class="spoke" cx="205" cy="62" r="54" />
  <text class="text" x="162" y="70">Search</text>
</svg>

Note:
21-22 min.

Say: "The durable model is hub and spokes. The site is the hub: identity, archive, source of truth. Protocols are spokes: RSS, Webmention, ActivityPub, AT Protocol, Nostr, whatever comes next."

Say: "This is not about one protocol winning. It is about not needing to reset identity and content every time a new network becomes important."

Transition: "So I want to end with a discussion question."

---

<div class="layout-big-text">

<h1>I want people to meet me where they are.</h1>
<p>I do not want my identity to live wherever they happen to meet me.</p>

</div>

Note:
22-23 min.

Say: "That is the central design principle."

Say: "If someone follows from Mastodon, great. If they read through RSS, great. If someday they see the same post through AT Protocol or Nostr, great."

Say: "But my home, identity, and archive should not depend on any one of those networks."

Transition: "Let's discuss."

---

## Discussion

<div class="layout-centered">

<h2>What should personal-site-native social interaction look like?</h2>

<p>Is the inbox a Webmention endpoint, an ActivityPub inbox, email, a feed reader, a dashboard, or all of the above?</p>

</div>

Note:
23-25 min.

Ask: "What feels right or wrong about this architecture?"

Ask: "For Fediverse practitioners: what am I underestimating about moderation, identity, delivery, or client compatibility?"

Ask: "For IndieWeb folks: what is the right way to pull interactions back to the site without turning the site into a giant app?"

If time is short, end here. If there is more time, go to the appendix sections.

---

<!-- .slide: id="appendix" -->

## Appendix

<div class="layout-two-column">
<div>

<h3>Technical deep dives</h3>

<ul>
  <li><a href="#/endpoint-anatomy">A. Endpoint anatomy</a></li>
  <li><a href="#/static-first-details">B. Static-first details</a></li>
  <li><a href="#/delivery-pipeline">C. Delivery pipeline</a></li>
  <li><a href="#/native-mapping">D. Native content mapping</a></li>
</ul>

</div>
<div>

<h3>Strategy deep dives</h3>

<ul>
  <li><a href="#/cost-model">E. Cost model</a></li>
  <li><a href="#/mastodon-migration">F. Mastodon migration</a></li>
  <li><a href="#/multi-protocol-future">G. Multi-protocol future</a></li>
</ul>

</div>
</div>

Note:
Appendix index.

Use the down arrow on appendix section slides for technical subslides.

---

<!-- .slide: id="endpoint-anatomy" -->

## Appendix A: Endpoint anatomy

<div class="layout-centered">

<p>What does the site expose so Fediverse software can understand it?</p>

</div>

--

<div class="fediverse-appendix-text">

## WebFinger

```text
GET /.well-known/webfinger?resource=acct:lqdev@lqdev.me
```

Returns account discovery data pointing to the ActivityPub actor.

This is what makes `@lqdev@lqdev.me` searchable from Mastodon-style clients.

</div>

--

<div class="fediverse-appendix-text">

## Actor

```text
GET /api/activitypub/actor
Accept: application/activity+json
```

Returns the followable profile document: id, preferred username, inbox, outbox, followers, following, and public key.

The public key lets remote servers verify signed outbound activities.

</div>

--

<div class="fediverse-appendix-text">

## Outbox

```text
GET /api/activitypub/outbox
GET /api/activitypub/outbox?page=1
```

The root outbox is an `OrderedCollection`.

Pages are `OrderedCollectionPage` documents with generated activities.

Pagination keeps the outbox compatible as the site grows.

</div>

--

<div class="fediverse-appendix-text">

## Inbox

```text
POST /api/activitypub/inbox
```

Receives Follow and Undo activities.

Verifies HTTP signatures, stores follower state, and sends Accept activities back to the remote actor.

The current implementation is intentionally narrow: publishing and follows first.

</div>

---

<!-- .slide: id="static-first-details" -->

## Appendix B: Static-first details

<div class="layout-centered">

<p>The site stays static by default; only protocol edges become dynamic.</p>

</div>

--

<div class="fediverse-appendix-text">

## Build-time outputs

- HTML pages for people.
- RSS feeds for feed readers.
- ActivityPub activity JSON for Fediverse software.
- Outbox root and paginated outbox pages.
- Individual activity files with stable hash-based IDs.
- Delivery queue metadata for recent posts.

The F# generator does the repeatable work once during build.

</div>

--

<div class="fediverse-appendix-text">

## Runtime edges

- WebFinger and actor endpoints need correct JSON/content-type responses.
- Inbox needs to accept POST requests.
- Delivery needs signing and retry behavior.
- Follower state needs persistence.
- Activity dereferencing needs an HTTP proxy because Azure Static Web Apps separates static content from Functions.

</div>

--

<div class="fediverse-appendix-text">

## Why the proxy exists

Static activity files live at:

```text
/activitypub/activities/{hash}.json
```

Fediverse clients fetch:

```text
/api/activitypub/activities/{hash}
```

The Function fetches the static JSON from the CDN, returns `application/activity+json`, caches it, and unwraps `Create` activities to `Note` or `Article` objects for Mastodon URL-search compatibility.

</div>

---

<!-- .slide: id="delivery-pipeline" -->

## Appendix C: Delivery pipeline

<div class="layout-centered">

<p>How a new website post reaches follower inboxes.</p>

</div>

--

<div class="fediverse-appendix-text">

## Publish sequence

1. Write markdown.
2. Push to `main`.
3. GitHub Actions builds the F# site.
4. Build generates ActivityPub JSON.
5. Azure Static Web Apps deploys static content and API Functions.
6. Workflow queues recent Create activities.
7. Delivery worker signs and POSTs to follower inboxes.

</div>

--

<div class="fediverse-appendix-text">

## Shared inbox optimization

If 100 followers are on the same Mastodon server, naive delivery sends 100 POST requests.

Shared inbox delivery can send one POST to the server's shared inbox.

The queue groups followers by `sharedInbox` when available, falling back to individual inbox URLs.

</div>

--

<div class="fediverse-appendix-text">

## State tables

- `followers`: actor URL, inbox, shared inbox, display name, follow activity.
- `deliverystatus`: per-activity delivery attempts and outcomes.
- Queue messages: pending delivery work with retry behavior.

The website remains static, but federation state lives in cheap table/queue storage.

</div>

---

<!-- .slide: id="native-mapping" -->

## Appendix D: Native content mapping

<div class="layout-centered">

<p>How site semantics become ActivityStreams semantics.</p>

</div>

--

<div class="fediverse-appendix-text">

## Response mapping

| Site intent | Site metadata | ActivityPub shape |
| --- | --- | --- |
| Reply | `response_type: reply`, `targeturl` | `Create` + `Note.inReplyTo` |
| Reshare | `response_type: reshare` | `Announce` |
| Star | `response_type: star` | `Like` |
| Bookmark | `response_type: bookmark` | `Create` + `Note` + `Link` attachment |

</div>

--

<div class="fediverse-appendix-text">

## Web-wide responses

The target URL can be any URL:

- A Fediverse object.
- A blog post.
- A news article.
- A video.
- A GitHub issue.

If the target is ActivityPub-discoverable, clients can do richer threading or notifications. If it is not, the semantic link still points back to the open web.

</div>

--

<div class="fediverse-appendix-text">

## Media and reviews

Media uses `Note` plus typed attachments because that renders more reliably in Mastodon/Pixelfed-style clients than standalone `Image` or `Video` objects.

Reviews can carry Schema.org-flavored rating and item metadata, preserving more of the site's structured content.

The principle: prefer native client behavior, but keep the site as the canonical source.

</div>

---

<!-- .slide: id="cost-model" -->

## Appendix E: Cost model

<div class="layout-centered">

<p>Why this is cheaper than running a single-user Mastodon instance.</p>

</div>

--

<div class="fediverse-appendix-text">

## Cost levers

- Static CDN serves almost all read traffic.
- Build-time generation avoids runtime rendering.
- Functions run only on protocol requests.
- Table Storage stores small follower/delivery records.
- Queue Storage smooths delivery fan-out.
- Key Vault stores signing keys without committing secrets.

The architecture pays for small edges, not a full always-on social app.

</div>

--

<div class="fediverse-appendix-text">

## The real comparison

This is not a replacement for community Mastodon hosting.

It is a replacement for the pieces I personally needed from a single-user instance:

- Fediverse identity.
- Followability.
- Publishing into follower timelines.
- Domain-owned archive and canonical content.

</div>

---

<!-- .slide: id="mastodon-migration" -->

## Appendix F: Mastodon migration

<div class="layout-centered">

<p>Moving identity is not only a protocol problem.</p>

</div>

--

<div class="fediverse-appendix-text">

## Migration checklist

- Export the Mastodon archive.
- Preserve bookmarks.
- Preserve follows, ideally as OPML/RSS subscriptions where possible.
- Communicate the new canonical Fediverse handle.
- Decide what old profile and redirects should say.
- Rebuild notification habits outside the Mastodon UI.

</div>

--

<div class="fediverse-appendix-text">

## What I would miss

The timeline is not just infrastructure. It is discovery, ambience, and social context.

Replacing a single-user instance with a website removes maintenance burden, but it also removes an app experience.

The open question is which parts should come back as site-native tools, feed-reader workflows, or protocol bridges.

</div>

---

<!-- .slide: id="multi-protocol-future" -->

## Appendix G: Multi-protocol future

<div class="layout-centered">

<p>The point is not ActivityPub maximalism.</p>

</div>

--

<div class="fediverse-appendix-text">

## Spokes, not new homes

- RSS for subscription.
- Webmention for website-to-website notification.
- ActivityPub for Fediverse follow and delivery.
- AT Protocol for data-repo and handle-oriented social applications.
- Nostr for key/event/relay-based publishing.

The source model should live in the site, not in each adapter.

</div>

--

<div class="fediverse-appendix-text">

## Design risk

Every protocol has its own vocabulary and client expectations.

If each adapter grows its own model, the site becomes a pile of bespoke bridges.

The better direction is a shared internal content/social model that can project into protocol-native shapes.

</div>

--

<div class="layout-big-text">

<h1>The site is the home.</h1>
<p>Protocols are ways to be a good guest elsewhere.</p>

</div>

Note:
Final appendix close.

Say: "That is the story I want the architecture to tell."

