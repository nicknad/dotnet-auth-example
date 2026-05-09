# 0001 - Abstraction - Stays easy to modify

**Status:** Accepted  
**Date:** 2026-04-18  
**Author:** Nick

## Context
The project is about user authentification and user management. To make sure we can exchange technical details both UserStorage,
the used Cache implementation and Token Handling will stay behind a abstraction / interface.
Current names:
- IUserStorage
- ITokenHandler
- ICacheService

## Decision
Due to time pressure no other options were evaluated.

## Consequences
Unclear

---

*Once accepted, do not modify — create a superseding ADR instead.*
