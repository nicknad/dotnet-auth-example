# Multi-stage build for smaller image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Healthcheck configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy source code
COPY . .

RUN dotnet restore "src/Auth.Api/Auth.Api.csproj"

RUN dotnet publish "src/Auth.Api/Auth.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-self-contained \
    /p:UseAppHost=false

# Final stage - minimal runtime image
FROM base AS final

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy published files from publish stage
COPY --from=build /app/publish .

# Ensure the app user has permissions to write in /app (for SQLite)
RUN chown -R app:app /app

USER app

# Set environment variables for production
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:8080

# Run the application
ENTRYPOINT ["dotnet", "Auth.Api.dll"]
