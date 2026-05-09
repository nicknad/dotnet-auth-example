# 0005 - Testing Strategy (Integration Tests over Unit Tests)

**Status:** Accepted  
**Date:** 2026-04-18  
**Author:** Nick

## Context
Unit tests vs. integration tests

Unit Tests:
Concept:
Test isolated components with mocked dependencies.

Pro's:
- Fast execution
- Precise failure localization
- Good for pure business logic

Con's:
- Limited confidence in full system behavior
- Requires extensive mocking
- Can diverge from real runtime behavior


Integration Tests:
Concept:
Test the full application stack (HTTP → middleware → services → database).

Pro's:
- High confidence in real behavior
- Covers authentication, authorization, serialization, and persistence
- Detects misconfigurations and wiring issues

Con's:
- Slower execution
- Harder to isolate failures
- Requires environment setup


Hybrid Approach (Integration + selective mocking):
Concept:
Run full pipeline while mocking external or non-deterministic dependencies (e.g., cache, external services).

Pro's:
- Maintains realistic execution
- Reduces flakiness
- Keeps tests deterministic

Con's:
- More complex test setup
- Still slower than unit tests

## Decision
Primary focus on integration tests, with selective mocking of infrastructure boundaries

## Consequences
Tests validate the real request pipeline, including authentication, middleware, and database interactions, increasing confidence in correctness.

Reduced reliance on unit tests lowers maintenance overhead for mocks and avoids testing implementation details.

Test execution time increases but remains acceptable for the project scope.

BenchmarkDotNet is used to complement testing by validating performance-critical paths (e.g., hashing, token validation).

If the codebase grows, targeted unit tests for complex business logic may be introduced to improve failure isolation.

---

*Once accepted, do not modify — create a superseding ADR instead.*
