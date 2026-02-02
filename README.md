# SensorHub

[![CI](https://github.com/LukasMicek/SensorHub/actions/workflows/ci.yml/badge.svg)](https://github.com/LukasMicek/SensorHub/actions/workflows/ci.yml)

IoT sensor data API built with .NET 8. Devices send temperature/humidity readings, the system stores them and triggers alerts when thresholds are exceeded.

## Features

- JWT authentication (Admin/User roles)
- Device management with API keys
- Sensor data ingestion
- Threshold-based alerts
- PostgreSQL + EF Core

## Quick Start (Docker)

```bash
cp .env.example .env
# Edit .env with your values

docker-compose up --build
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
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

## API Quick Flow

```bash
# 1. Login
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_ADMIN_EMAIL","password":"YOUR_ADMIN_PASSWORD"}'

# 2. Create device (use token from step 1)
curl -X POST http://localhost:5000/api/v1/devices \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Sensor-01","location":"Warehouse"}'

# 3. Generate device API key
curl -X POST http://localhost:5000/api/v1/devices/{id}/api-key \
  -H "Authorization: Bearer YOUR_TOKEN"

# 4. Ingest reading (use device key)
curl -X POST http://localhost:5000/api/v1/readings/ingest \
  -H "X-Device-Key: YOUR_DEVICE_KEY" \
  -H "Content-Type: application/json" \
  -d '{"temperature":25.5,"humidity":60}'

# 5. Check alerts
curl http://localhost:5000/api/v1/alerts \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Tests

```bash
dotnet test
```

- **Unit tests**: Fast, use in-memory DB
- **Integration tests**: Use [Testcontainers](https://testcontainers.com/) to spin up real PostgreSQL - just have Docker running

75 tests total.

## CI

GitHub Actions runs build + tests on every push/PR to `main`. Testcontainers works out of the box on GitHub runners.

See [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

## Postman

Import [`postman/SensorHub.postman_collection.json`](postman/SensorHub.postman_collection.json) into Postman.

Run requests 1-7 in order for the happy path. Variables (`jwt`, `deviceId`, `deviceApiKey`) are set automatically via test scripts.

Default `baseUrl`: `http://localhost:5000`

## Health Endpoint

```bash
curl http://localhost:5000/health
# {"status":"healthy"}
```

## Project Structure

```
SensorHub/
├── src/SensorHub.Api/        # Web API
├── tests/SensorHub.Tests/    # Unit + integration tests
├── postman/                  # Postman collection
├── docker-compose.yml
└── .env.example
```

## Tech Stack

.NET 8 · EF Core · PostgreSQL · ASP.NET Identity · JWT · xUnit · Testcontainers · Docker
