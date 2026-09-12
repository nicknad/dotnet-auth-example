# Architecture Overview

## Approach
This solution uses a layered architecture for the Auth.Api service, with clear separation between API, Infrastructure, and shared common code. This structure was chosen to maximize maintainability, testability, and scalability. Each layer is responsible for a specific concern:

- **API Layer** (`Features/`, `Middleware/`, `Extensions/`): Exposes HTTP endpoints for authentication, user management, and health checks, plus the middleware pipeline.
- **Infrastructure Layer** (`Infrastructure/`, `Abstractions/`): Handles data access (Entity Framework Core), caching, token handling, and logging (Serilog) behind interfaces.
- **Common Layer** (`Common/`): Shared DTOs, constants, validation attributes and result types used across the API.

The project currently keeps business rules close to the API/Infrastructure boundary rather than in a separate Domain project; that refactoring can be introduced if the domain grows.

This approach enables isolated testing, easier onboarding, and flexibility for future enhancements.

## Rationale
Layered architecture is a proven pattern in .NET for building robust, modular, and testable applications. It allows for:
- Clear separation of concerns
- Easier refactoring and onboarding
- Improved testability (mocking infrastructure, focusing on business logic)
- Flexibility to swap dependencies

## Dependencies
**Auth.Api** uses:
- .NET 10
- ASP.NET Core (Web API)
- Entity Framework Core (with InMemory and Sqlite providers)
- Microsoft.AspNetCore.Identity (for user management)
- Microsoft.AspNetCore.Authentication.JwtBearer (JWT authentication)
- Serilog (logging)
- NetEscapades.AspNetCore.SecurityHeaders (security headers)
- Dependency Injection (built-in)

**Testing** uses:
- xUnit (test framework)
- Microsoft.AspNetCore.Mvc.Testing (integration testing)
- NSubstitute (mocking, benchmarks only)
- coverlet.collector (code coverage)
- BenchmarkDotNet (benchmarks)

All dependencies are selected for their maturity, support, and .NET 10 compatibility.

## Test Strategy

### Integration Tests
- Located in `tests/Auth.Tests/Integration/`
- Use `WebApplicationFactory` and custom fixtures to spin up the API in memory with an in-memory database
- Cover all major endpoints: login, logout, refresh token, user registration, user management (CRUD), and edge cases (invalid input, authorization, etc.)
- Test helpers abstract common flows (register, login, authenticated requests)
- Each fixture seeds roles and an admin user; production-style database seeding is opt-in via configuration
- A controllable `TimeProvider` lets tests exercise token expiry without waiting

### Security Tests
- Located in `tests/Auth.Tests/Security/`
- Use a custom Kestrel host with the same middleware pipeline and routes as the application, plus WebApplicationFactory-based tests for rate limiting measures
- Cover payload size limits, header limits, rate limiting and security headers

### Benchmarks
- Located in `tests/Auth.Benchmarks/`
- Use BenchmarkDotNet to measure performance of critical paths (JWT creation/validation, password hashing, claims extraction)

### Coverage
- Endpoint happy paths, negative cases and the main security boundaries are covered
- Known gaps: health endpoint behaviour beyond a smoke check, CORS configuration, IP blocking (POC only) and OpenAPI output
- Code coverage is collected with coverlet in CI

### Rationale
This strategy ensures:
- High confidence in correctness and security
- Fast feedback for regressions
- Realistic simulation of production scenarios
- Measurable performance for critical operations
