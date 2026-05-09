# 0004 - Identity Management (ASP.NET Core Identity)

**Status:** Accepted  
**Date:** 2026-04-18  
**Author:** Nick

## Context
Custom identity implementation vs. framework-provided solution

Custom Implementation:
Concept:
Manually implement user management, password hashing, login flows, and security controls.

Pro's:
- Full control over data model and behavior
- Minimal abstraction overhead

Con's:
- High risk of security flaws
- Reinventing well-known mechanisms (password hashing, lockout, etc.)
- Significant development effort


ASP.NET Core Identity:
Concept:
Framework providing user management, password hashing, and security primitives.

Pro's:
- Secure defaults (PBKDF2, salting, iteration management)
- Battle-tested implementation
- Integrated with ASP.NET authentication/authorization
- Supports extensibility (custom stores, claims, roles)

Con's:
- Additional complexity and abstraction
- Opinionated schema and flows
- Can be excessive for very small systems

## Decision
Use ASP.NET Core Identity with its database schema

## Consequences
Security-critical functionality (password hashing, credential validation) is delegated to a well-tested framework, reducing implementation risk.

The system inherits Identity’s schema and conventions, which may introduce constraints on customization.

Some flexibility is sacrificed in favor of correctness and reduced attack surface.

Integration complexity increases slightly, but aligns well with JWT-based authentication and role/claim handling.

If requirements diverge significantly (e.g., external identity providers, custom auth flows), Identity can be extended or partially replaced.

---

*Once accepted, do not modify — create a superseding ADR instead.*
