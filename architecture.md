# Architecture Overview

## Approach
This solution uses a layered architecture for the Auth.Api service, with clear separation between API, Infrastructure, and Domain layers. This structure was chosen to maximize maintainability, testability, and scalability. Each layer is responsible for a specific concern:

- **API Layer**: Exposes HTTP endpoints for authentication, user management, and health checks.
- **Infrastructure Layer**: Handles data access (Entity Framework Core), caching, logging (Serilog), and external integrations.
- **Domain Layer**: Encapsulates business logic and domain entities, independent of infrastructure.

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
- NSubstitute (mocking)
- coverlet.collector (code coverage)
- BenchmarkDotNet (benchmarks)

All dependencies are selected for their maturity, support, and .NET 10 compatibility.

## Test Strategy

### Integration Tests
- Located in `tests/Auth.Tests/Integration/`
- Use `WebApplicationFactory` and custom fixtures to spin up the API with an in-memory database and real HTTP calls
- Cover all major endpoints: login, logout, refresh token, user registration, user management (CRUD), and edge cases (invalid input, authorization, etc.)
- Test helpers abstract common flows (register, login, authenticated requests)
- Database is seeded with roles and an admin user for role-based scenarios
- Security and payload limits are tested (rate limiting, payload size, etc.)

### Security Tests
- Located in `tests/Auth.Tests/Security/`
- Use a custom Kestrel host to test rate limiting, payload size, and security headers
- Ensure the API enforces security policies under realistic conditions

### Benchmarks
- Located in `tests/Auth.Benchmarks/`
- Use BenchmarkDotNet to measure performance of critical paths (JWT creation/validation, password hashing, claims extraction)

### Coverage
- All endpoints, edge cases, and security boundaries are covered
- Tests validate both positive and negative scenarios (success, unauthorized, forbidden, bad request, etc.)
- Code coverage is collected with coverlet

### Rationale
This strategy ensures:
- High confidence in correctness and security
- Fast feedback for regressions
- Realistic simulation of production scenarios
- Measurable performance for critical operations
