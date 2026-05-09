# 0002 - Token choice

**Status:** Accepted  
**Date:** 2026-04-18  
**Author:** Nick

## Context
JWT vs Server-mapped Token

Server-Mapped:
Concept:
A random identifier (e.g., 128-bit) mapped to server-side state.
Flow:

Client sends token
Server performs lookup (DB / Redis)

Pro's:
Easy to revoke
No embedded data

Con's:
Requires network lookup


JWT (Json Web Token)
Concept:
Self contained token

Pro's:
- Token contains necassary information, no lookup needed
- easy usage in distirbuted context

Con's:
- Complex revocation

## Decision
JWT

## Consequences
Implementation complexity increases, especially to counteract some downsides (via short lifetimes + refreshtoken).
If more information for the production environment are clear this decision could easily be superseding.

---

*Once accepted, do not modify — create a superseding ADR instead.*
