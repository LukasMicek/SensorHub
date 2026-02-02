# SensorHub

[![CI](https://github.com/LukasMicek/SensorHub/actions/workflows/ci.yml/badge.svg)](https://github.com/LukasMicek/SensorHub/actions/workflows/ci.yml)

IoT sensor data API built with .NET 8. Devices send temperature/humidity readings, the system stores them and triggers alerts when thresholds are exceeded.

## Features

- JWT authentication with Admin/User roles
- Device management (create, list, generate API keys)
- Sensor data ingestion via device API keys
- Threshold-based alerting (temperature/humidity)
- Swagger UI for API exploration
- Static demo page for quick testing
- Postman collection with auto-set variables
- Unit + integration tests with Testcontainers

## Showcase

| Resource | URL |
|----------|-----|
| Demo Page | http://localhost:5000/demo.html |
| Swagger | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |
| Postman | [`postman/SensorHub.postman_collection.json`](postman/SensorHub.postman_collection.json) |

## Quick Start (Docker)

```bash
cp .env.example .env
# Edit .env with your values

docker-compose up --build
```

## Configuration

Create `.env` from the template:

```env
POSTGRES_DB=sensorhub
POSTGRES_USER=sensorhub
POSTGRES_PASSWORD=YOUR_DB_PASSWORD
JWT_SECRET=YOUR_JWT_SECRET_MIN_32_CHARS

# Optional: seed admin (Development only)
ASPNETCORE_ENVIRONMENT=Development
SEED_ADMIN=true
SEED_ADMIN_EMAIL=YOUR_ADMIN_EMAIL
SEED_ADMIN_PASSWORD=YOUR_ADMIN_PASSWORD
```

**Local development (Windows PowerShell):**

```powershell
$env:Jwt__Secret = "YOUR_JWT_SECRET_MIN_32_CHARS"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Database=sensorhub;Username=sensorhub;Password=YOUR_DB_PASSWORD"
dotnet run --project src/SensorHub.Api
```

## Demo Page

1. Start the API: `docker-compose up`
2. Get a JWT token (Postman or curl)
3. Open http://localhost:5000/demo.html
4. Paste JWT and click "Save to localStorage"
5. Explore devices, readings, and alerts

## Postman

Import [`postman/SensorHub.postman_collection.json`](postman/SensorHub.postman_collection.json).

Run requests 1-7 in order. Variables are set automatically.

## Tests

```bash
dotnet test
```

- **Unit tests** - in-memory DB, fast
- **Integration tests** - [Testcontainers](https://testcontainers.com/) spins up real PostgreSQL

## CI

GitHub Actions workflow on every push/PR to `master`:
1. Restore NuGet packages
2. Build in Release mode
3. Run all tests (unit + integration)

See [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

## Design Notes

- **JWT + ASP.NET Identity** - Standard approach for user auth, easy role management
- **Device API keys** - Simpler than JWT for IoT devices, just a header
- **Service layer** - Keeps controllers thin, business logic testable in isolation
- **Testcontainers** - Integration tests use real PostgreSQL, catches real bugs
- **ProblemDetails** - Consistent error responses across all endpoints

## Project Structure

```
SensorHub/
├── src/SensorHub.Api/
│   ├── Controllers/      # API endpoints
│   ├── Services/         # Business logic
│   ├── Data/             # EF Core DbContext + migrations
│   ├── Models/           # Entities + DTOs
│   └── Auth/             # Device API key handler
├── tests/SensorHub.Tests/
├── postman/
├── docker-compose.yml
└── .env.example
```

## Tech Stack

.NET 8 · EF Core · PostgreSQL · ASP.NET Identity · JWT · xUnit · Testcontainers · Docker
