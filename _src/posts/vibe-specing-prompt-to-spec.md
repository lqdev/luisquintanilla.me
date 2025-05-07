---
post_type: "article" 
title: "Vibe-Specing - From concepts to specification"
description: "An exploration into using AI for bringing concepts together into a specification"
published_date: "2025-05-06 20:40"
tags: ["dweb","standards","ai","decentralization","protocol"]
---

## Introduction

Code generation is a common use case for AI. What about the design process that comes before implementation? Personally, I've found that AI excels not just at coding, but also helping formalize abstract ideas into concrete specifications. This post explores how I used AI-assisted design to transform a collection of loosely related concepts into a technical specification for a new system made up of those concepts.

## Specifications

Generally, I've had mixed success with vibe-coding (the practice of describing what you want in natural language and having AI generate the corresponding code). However, it's something that I'm constantly working on getting better at. Also, with tooling integrations like MCP, I can ground responses and supplement my prompts using external data.

What I find myself being more successful with is using AI to explore ideas and then formalizing those ideas into a specification. Even in the case of vibe-coding, what you're doing with your prompts is building a specification in real-time. 

I'd like to think that eventually I'll get to the vibe-coding part but before diving straight into the code, I'd like to spend time in the design phase. Personally, this is also the part that I find the most fun because you can throw wild things at the wall. It's not until you implement them that you actually validate whether some of those wild ideas are practical. But I find the design phase a ton of fun.

The result of my latest vibe-specing adventure is what I'm calling the [InterPlanetary Knowledge System (IPKS)](https://github.com/lqdev/IPKS).

## From concept to specification

Lately, I've been thinking a lot about knowledge. Some concepts that have been in my head are those of **non-linear publishing** (creating content that can be accessed in any order with multiple entry points, like wikis or hypertext) and **distributed cognition** (the idea that human knowledge and cognitive processes extend beyond the individual mind to include interactions with other people, tools, and environments).
Related to those concepts, I've also been thinking about how **digital gardens** (personal knowledge bases that blend note-taking, blogging, and knowledge management in a non-linear format) and **Zettelkasten** (a method of note-taking where ideas are captured as atomic notes with unique identifiers and explicit connections) are ways to capture and organize knowledge.

One other thing that I'm amazed by is the powerful concept of a **hyperlink** and how it makes the web open, decentralized, and interoperable. When paired with the **semantic web** (an extension of the web that provides a common framework for data to be shared across applications and enterprises), you have yourself a decentralized knowledgebase containing a lot of the world's knowledge.

At some point, IPFS (InterPlanetary File System, a protocol designed to create a permanent and decentralized method of storing and sharing files) joined this pool of concepts I had in my head.

These were all interesting concepts individually, but I knew there were connections but couldn't cohesively bring them together. That's where AI-assisted specification design came in.

Below is a summary of the collaborative design interaction with Claude Sonnet 3.7 (with web search) that eventually led to the generation of the IPKS specifications. I haven't combed through them in great detail, but what they're proposing seems plausible. 

Overall, I'm fascinated by this interaction. Whether or not IPKS ever becomes a reality, the process of using AI to transform abstract concepts into concrete specifications seems like a valuable and fun design approach that I'll continue to refine and include as part of my vibe-coding sessions. 

---

## Initial Exploration: IPFS and Knowledge Management

Our conversation began with exploring IPFS (InterPlanetary File System) and its fundamental capabilities as a content-addressed, distributed file system. We recognized that while IPFS excels at storing and retrieving files in a decentralized manner, it needed extensions to support knowledge representation, trust, and semantics.

Key insights from this stage:
- IPFS provides an excellent foundation with content addressing through CIDs
- Content addressing enables verification but doesn't inherently provide meaning
- Moving from document-centric to idea-centric systems requires additional layers

## Knowledge Management Concepts

We explored established knowledge management approaches, particularly:

### Zettelkasten
The Zettelkasten method contributed these important principles:
- Atomic units of knowledge (one idea per note)
- Explicit connections between ideas
- Unique identifiers for each knowledge unit
- Emergent structure through relationship networks

### Digital Gardens
The Digital Garden concept provided these insights:
- Knowledge in various stages of development
- Non-linear organization prioritizing connections
- Evolution of ideas over time
- Public visibility of work-in-progress thinking

These personal knowledge management approaches helped us envision how similar principles could work at scale in a distributed system.

## The "K" in IPKS

When we proposed replacing "IPFS" with "IPKS" (changing File → Knowledge), we recognized the need to define what makes knowledge different from files. This led to identifying several key requirements:

1. **Semantic meaning** - Knowledge needs explicit relationships and context
2. **Provenance and trust** - Knowledge requires verifiable sources and expertise
3. **Evolution** - Knowledge changes over time while maintaining continuity
4. **Governance** - Knowledge exists in various trust and privacy contexts

These requirements shaped the layered architecture of the specifications.

## Distributed Cognition and Non-Linear Publishing

Our discussions about distributed cognition highlighted how thinking processes extend beyond individual minds to include:
- Interactions with other people
- Cultural artifacts and tools
- Physical and digital environments
- Social and technological systems

This concept directly influenced the IPKS design by emphasizing:
- Knowledge as a collective, distributed resource
- The need for attribution and expertise verification
- The value of connecting knowledge across boundaries
- The role of tools in extending human cognition

Similarly, non-linear publishing concepts shaped how we approached knowledge relationships and navigation in IPKS, moving away from sequential formats toward interconnected networks of information.

## Web3 Technologies Integration

Our exploration of complementary technologies led to incorporating:

### Decentralized Identifiers (DIDs)
DIDs provided the framework for:
- Self-sovereign identity for knowledge contributors
- Cryptographic verification of authorship
- Persistent identification across systems
- Privacy-preserving selective disclosure

### Verifiable Credentials (VCs)
Verifiable Credentials offered mechanisms for:
- Expertise validation without central authorities
- Domain-specific qualification verification
- Credential-based access control
- Trust frameworks for knowledge contributors

### Semantic Web (RDF/OWL)
Semantic Web standards influenced:
- Relationship types between knowledge nodes
- Ontologies for domain knowledge representation
- Query patterns for knowledge discovery
- Interoperability with existing knowledge systems

## Business Context: Supply Chain Example

Our conversation about supply chain management provided a concrete use case that helped ground the specifications in practical application. This example demonstrated how IPKS could address real-world challenges:

- **Material Provenance**: Using DIDs and verifiable credentials to establish trusted material sources
- **Cross-Organization Collaboration**: Enabling knowledge sharing while respecting organizational boundaries
- **Regulatory Compliance**: Creating verifiable documentation of compliance requirements
- **Expertise Validation**: Ensuring contributors have appropriate qualifications for their roles
- **Selective Disclosure**: Balancing transparency with competitive confidentiality

This business context helped shape the Access Control & Privacy specification in particular, highlighting the need for nuanced governance models.

## Technical Implementation Considerations

As we moved from abstract concepts to specifications, several technical considerations emerged:

1. **Building on IPLD**: Recognizing that InterPlanetary Linked Data (IPLD) already provided foundational components for structured, linked data in content-addressed systems

2. **Modular Specification Design**: Choosing to create multiple specifications rather than a monolithic standard to enable incremental implementation and adoption

3. **Backward Compatibility**: Ensuring IPKS could work with existing IPFS/IPLD infrastructure

4. **Extensibility**: Designing for future enhancements like AI integration, advanced semantic capabilities, and cross-domain knowledge mapping

## The Path Forward

The IPKS specifications represent a synthesis of our conceptual exploration, grounded in:
- Established knowledge management practices
- Decentralized web technologies
- Real-world business requirements
- Technical feasibility considerations

Moving from concept to implementation will require:
1. Reference implementations of the core specifications
2. Developer tools and libraries to simplify adoption
3. Domain-specific extensions for particular use cases
4. Community building around open standards

By building on the combined strengths of IPFS, DIDs, VCs, and semantic web technologies, IPKS creates a framework for distributed knowledge that balances openness with trust, flexibility with verification, and collaboration with governance.
