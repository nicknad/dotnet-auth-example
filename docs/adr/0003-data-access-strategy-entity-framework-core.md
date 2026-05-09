# 0003 - Data Access Strategy (Entity Framework Core)

**Status:** Accepted  
**Date:** 2026-04-18  
**Author:** Nick

## Context
ORM vs. micro-ORM vs. raw SQL

Raw SQL:
Concept:
Direct SQL execution via ADO.NET or similar.

Pro's:
- Maximum control over queries
- Optimal performance possible

Con's:
- High development overhead
- Error-prone (manual mapping, SQL duplication)
- Harder to maintain and evolve


Micro-ORM (e.g., Dapper):
Concept:
Lightweight mapping between SQL and objects.

Pro's:
- High performance
- More control than full ORM

Con's:
- Manual query management required
- No built-in change tracking
- More boilerplate for complex domains


Full ORM (Entity Framework Core):
Concept:
Object-relational mapping with change tracking, LINQ querying, and migrations.

Pro's:
- Rapid development
- Built-in migrations
- Strong integration with ASP.NET ecosystem
- Change tracking and relationship handling

Con's:
- Less control over generated SQL
- Potential performance overhead
- Risk of inefficient queries if misused

## Decision
Entity Framework Core

## Consequences
Development speed is prioritized over raw performance and low-level control.  
The abstraction reduces boilerplate and simplifies persistence concerns, which is appropriate for an MVP.

Performance risks (e.g., N+1 queries, inefficient LINQ translation) must be actively managed and verified via benchmarks.

The decision avoids introducing an additional repository abstraction, as EF Core already fulfills that role.

If performance or query complexity becomes critical, selective fallback to raw SQL or Dapper is possible without replacing the entire data access layer.

---

*Once accepted, do not modify — create a superseding ADR instead.*
