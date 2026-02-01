# Architectural Decision Records (ADRs)

This directory contains Architectural Decision Records (ADRs) documenting important design decisions made in this project.

## What is an ADR?

An Architectural Decision Record (ADR) captures an important architectural decision made along with its context and consequences. ADRs provide a structured way to document and communicate significant design choices.

## ADR Template

We follow the [MADR (Markdown Architectural Decision Records)](https://adr.github.io/madr/) format. Each ADR includes:

- **Title**: A short, descriptive name for the decision
- **Status**: Draft, Proposed, Accepted, Deprecated, or Superseded
- **Context**: The issue motivating this decision
- **Decision**: The change being proposed or implemented
- **Consequences**: What becomes easier or more difficult because of this change

## Current ADRs

| ID | Title | Status | Date |
|----|-------|--------|------|
| [0001](0001-unified-content-processing.md) | Unified Content Processing with GenericBuilder | Accepted | 2025 |
| [0002](0002-indieweb-microformats.md) | IndieWeb Microformats Integration | Accepted | 2024 |
| [0003](0003-static-site-architecture.md) | F# Static Site Generator Architecture | Accepted | 2024 |
| [0004](0004-progressive-loading.md) | Progressive Loading for Large Content Volumes | Accepted | 2025 |
| [0005](0005-file-organization.md) | Repository File Organization | Accepted | 2026-02 |

## Creating a New ADR

1. Copy the template from `0000-template.md`
2. Rename to `NNNN-title-with-dashes.md` (use the next available number)
3. Fill in all sections
4. Submit for review via pull request
5. Update this README with the new entry

## References

- [ADR GitHub Organization](https://adr.github.io/)
- [MADR Template](https://adr.github.io/madr/)
- [Joel Parker Henderson's ADR Template](https://github.com/joelparkerhenderson/architecture-decision-record)
