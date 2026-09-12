# Auth Api - ASP.NET Core 10.0 Web API

An ASP.NET Core 10.0 Web API demonstrating authentication, authorization, and user management with security best practices.

The project implements the task using Minimal APIs, Entity Framework Core with SQLite, and JWT-based authentication. Minimal Api was the natural choice for a small, focused API, allowing us to avoid unnecessary boilerplate while still following clean architecture principles.

Abstractions are deliberately introduced to decouple core logic from infrastructure concerns, allowing replacement of implementations
(e.g., switching to Redis or another database) without affecting business logic.

State is encapsulated behind interfaces to improve testability and enable future extensions.

Given an incomplete system context, JWT-based authentication with token versioning was selected as a pragmatic and scalable default.
Rate limiting and request size limiting is applied to mitigate brute-force attacks.

The primary testing strategy focuses on integration tests to validate the full request pipeline, complemented by benchmarks for performance-critical paths.

## Features

This project implements a minimal API with:
- User registration and authentication
- User listing with pagination (Admin role)
- Editing and viewing on user profiles (user or admin)
- Soft deletion of users (owner or Admin role)
- Token authentication ( chosen JWT-based )
- Refresh tokens for session management
- Token versioning for session invalidation to counteract JWT statelessness
- Role-based access control (user or admin)
- DataAnnotations for validation
- Rate limiting, limit for requests size and request header count for security
- IP blocking middleware (POC, not fully implemented)

Missing features that could be added in the future:
- Password reset functionality
- Account lockout after multiple failed login attempts
- Multi-factor authentication (MFA)

## Architecture

Some decision are more detailed in the ADRs in the `docs/adr/` folder, but here is a high-level overview of the architecture and design decisions:
Given the timeframe the scope of documentation is very limited.

The project follows a clean architecture with separation of concerns:

- Separation into:
  - API (Features)
  - Infrastructure (EF Core, Caching, services)
  - Middleware (Security, rate limiting)

Folders are organized by feature and responsibility:

- **Common**: Shared utilities, constants, and DTOs
- **Abstractions**: Interface definitions for abstractions that would allow swapping implementations without changing the core logic
- **Features**: Contains API endpoints organized by feature (Auth, Users)
- **Infrastructure**: Data access, services
- **Middleware**: Custom middleware for security and rate limiting

If this were to be extended into a larger system, we would consider further modularization (e.g., separate projects for API, services, and data access) and more advanced patterns (e.g., CQRS, event sourcing) as needed.
Api Versioning would be added via version folders (e.g., `Features/v1/Auth/Login`) or via separate projects for each version.


### Project Structure

```
root/
  src/Auth.Api/
    Abstractions/      - Interface definitions for abstractions (e.g., IUserStorage, ITokenService)
    Common/            - Shared utilities, constants, and DTOs
    Extensions/        - Extension methods for service registration, middleware, etc.
    Features/
      Auth/Login/      - Login endpoint
      Auth/Logout/     - Logout endpoint
      Auth/Refresh/    - token refresh
      Users/Register/  - User registration
      Users/GetById/   - Get user by ID
      Users/List/      - List users with pagination
      Users/Patch/     - Update user profile
      Users/Delete/    - Soft delete user
      Health/          - Health check endpoint
    Infrastructure/
      Services/       - Service implementations
      Storage/        - User Storage / DB
    Middleware/       - Custom middleware (IP blocking, token validation)
    Migrations/       - EF Core migrations

  tests/
    Auth.Tests/       - Integration tests
    Auth.Benchmarks/  - Performance benchmarks
```

### Tests
Given the timeframe the tests are focused on integration testing of the API endpoints, covering:
- **Tests**: Integration tests for all API endpoints and security features
- **Benchmarks**: Performance benchmarks for critical paths (password hashing, token creation/validation)


### Technology Stack

Please check THIRD-PARTY-NOTICES.md for detailed license information on each dependency.

- **.NET 10.0** - Latest LTS framework (MIT)
- **Entity Framework Core 10** with SQLite (MIT)
- **JWT Bearer** authentication / **Microsoft.IdentityModel** (MIT)
- **xUnit v3** for testing (MIT)
- **BenchmarkDotNet** for performance testing (MIT)
- **NSubstitute** for mocking in benchmarks (BSD-3-Clause)
- **Serilog** for structured logging (Apache 2.0)
- **SonarAnalyzers** for code quality (SONAR Source-Available License v1.0)
- **StyleCop Analyzers** for coding conventions (MIT)
- **AsyncFixer** for async code analysis (Apache 2.0)
- **NetEscapades.AspNetCore.SecurityHeaders** Security headers middleware (MIT)

### Security Concepts

#### Password Handling
- Uses `Microsoft.AspNetCore.Identity` password hasher
- Follows RFC 8018: PBKDF2 with SHA256, 100,000 iterations (Identity default)

#### JWT Tokens
- Access tokens: 15-minute expiry
- Refresh tokens: 7-day expiry (stored in DB)
- Token versioning for session invalidation
- Signed with HMAC-SHA256

#### Token Versioning
Each user has a `TokenVersion` field that increments on:
- User deletion
- Password change
- Logout (all sessions)

This allows invalidating all user sessions without storing a token blacklist.

---

#### Rate Limiting
- Per-IP rate limiting middleware

#### IP Blocking (Only POC)
- Middleware blocks malicious IPs

### Coding Conventions

- **PascalCase**: Classes, methods, properties, constants
- **camelCase**: Local variables and parameters
- **Underscore prefix**: Private fields (`_fieldName`)
- **sealed** classes by default
- **record** types for immutable DTOs
- Nullable reference types enabled
- 4-space indentation

## Running the Project

```bash
# Restore and build
dotnet restore
dotnet build --configuration Release

# Run the API
dotnet run --project src/Auth.Api

# Run tests
dotnet test --configuration Release

# Run tests and save results to results/tests/
dotnet test --configuration Release --results-directory results/tests --logger "trx;LogFileName=results.trx"

# Run benchmarks and save results to results/benchmarks/
dotnet run --project tests/Auth.Benchmarks/Auth.Benchmarks.csproj --configuration Release -- results/benchmarks

# Run an individual benchmark (BenchmarkDotNet arguments are forwarded)
dotnet run --project tests/Auth.Benchmarks/Auth.Benchmarks.csproj --configuration Release -- results/benchmarks --filter *JwtCreationBench*

# Run with Docker (JWT_KEY must be at least 32 characters)
$env:JWT_KEY = "replace-with-a-32-plus-character-secret"
docker compose up --build

```

In the Development environment the database is migrated and seeded with `admin@example.com` / `Admin123!`; both values can be changed via `Seed:AdminEmail` and `Seed:AdminPassword`. Seeding is disabled by default outside Development, while database migrations always run on startup.---

## API Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/auth/login` | User login | No |
| POST | `/api/v1/auth/refresh` | Refresh token | No |
| POST | `/api/v1/auth/logout` | Logout | Yes |
| POST | `/api/v1/users/register` | Register new user | No |
| GET | `/api/v1/users` | List users | Yes (Admin) |
| GET | `/api/v1/users/{id}` | Get user by ID | Yes |
| PATCH | `/api/v1/users/{id}` | Update user | Yes |
| DELETE | `/api/v1/users/{id}` | Soft delete user | Yes (owner or Admin) |

`GET /api/v1/users` supports `page` (0-based), `pageSize` (1-50), `name`, `email` and `role` query parameters; non-admin callers always receive only their own profile.

|Method| Endpoint | Description |
|--------|----------|-------------|
| Get | `/health` | Health check endpoint |
| Get | `/openapi/v1.json` | OpenAPI specification (only for development) |

---

## Future Extensions

### Security
- HTTPS enforcement with HSTS
- additional logging and alerting on security events (e.g., multiple failed login attempts, suspicious IP activity)
- additional protection against DDoS attacks (e.g., IP reputation checks)
- Multi-factor authentication (TOTP)
- Account lockout after failed attempts
- Notification of users on suspicious activity
- Notification of users on password changes or logins from new devices

### If migrated to distributed system
- load balancing and horizontal scaling
- Distributed token revocation (e.g., using Redis or a distributed cache)
- Redis-backed caching
- Distributed rate limiting
- Database connection pooling (especially split read and write of db)

### Monitoring
- Application metrics (OpenTelemetry tracing / Prometheus)
- logging exports to a centralized logging system (e.g., ELK stack, Seq)
- event publishing for critical events (e.g., user registration, login, password changes) to a monitoring service

### API Enhancements
- Batch endpoints

### Infrastructure
- extensive CI/CD pipelines / github workflows for automated testing, benchmarking, and deployment
- Environment-specific configuration


## Missing Quality of Life Improvements
- More comprehensive documentation (e.g., API docs, architecture diagrams)
- Build scripts for easier setup and deployment
- github workflows for automated testing and benchmarking on pull requests and commits
- Test coverage reports and code quality metrics
- Regression quality control with benchmarks and tests on every commit (automated)
