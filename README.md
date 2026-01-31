# SensorHub

[![CI](https://github.com/LukasMicek/SensorHub/actions/workflows/ci.yml/badge.svg)](https://github.com/YOUR_USERNAME/SensorHub/actions/workflows/ci.yml)

A .NET 8 Web API for IoT sensor data ingestion with JWT authentication, device API keys, and threshold-based alerting.

## Features

- **Authentication**: JWT-based auth with Admin/User roles
- **Device Management**: Create devices and generate API keys
- **Data Ingestion**: Ingest temperature/humidity readings via device API keys
- **Alerting**: Configurable threshold-based alert rules
- **PostgreSQL**: EF Core with migrations

## Prerequisites

- [Docker](https://www.docker.com/get-started) and Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)

## Quick Start with Docker

```bash
# Clone and navigate to project
cd SensorHub

# Copy environment template and configure
cp .env.example .env
# Edit .env and set your secrets (JWT_SECRET, POSTGRES_PASSWORD, etc.)

# Start the application
docker-compose up --build

# API is available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

## Environment Configuration

Copy `.env.example` to `.env` and configure:

| Variable | Required | Description |
|----------|----------|-------------|
| `POSTGRES_DB` | Yes | Database name |
| `POSTGRES_USER` | Yes | Database user |
| `POSTGRES_PASSWORD` | Yes | Database password |
| `JWT_SECRET` | Yes | JWT signing key (min 32 chars) |
| `JWT_ISSUER` | No | JWT issuer (default: SensorHub) |
| `JWT_AUDIENCE` | No | JWT audience (default: SensorHub) |
| `ASPNETCORE_ENVIRONMENT` | No | Environment (default: Production) |
| `SEED_ADMIN` | No | Enable admin seeding (default: false) |
| `SEED_ADMIN_EMAIL` | No | Admin email (only used if SEED_ADMIN=true) |
| `SEED_ADMIN_PASSWORD` | No | Admin password (only used if SEED_ADMIN=true) |

## Seeding Admin User (Development Only)

Admin seeding only runs when **both** conditions are met:
1. `ASPNETCORE_ENVIRONMENT=Development`
2. `SEED_ADMIN=true`

Example `.env` for local development:
```
POSTGRES_DB=sensorhub
POSTGRES_USER=sensorhub
POSTGRES_PASSWORD=<your-db-password>
JWT_SECRET=<your-secret-min-32-chars>
ASPNETCORE_ENVIRONMENT=Development
SEED_ADMIN=true
SEED_ADMIN_EMAIL=admin@sensorhub.local
SEED_ADMIN_PASSWORD=<your-admin-password>
```

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/v1/auth/login` | None | Login and get JWT token |
| POST | `/api/v1/auth/register` | None | Register new user |
| POST | `/api/v1/auth/assign-role` | Admin | Assign role to user |
| POST | `/api/v1/devices` | Admin | Create a device |
| GET | `/api/v1/devices` | Admin | List all devices |
| POST | `/api/v1/devices/{id}/api-key` | Admin | Generate device API key |
| POST | `/api/v1/readings/ingest` | Device Key | Ingest sensor reading |
| GET | `/api/v1/devices/{id}/readings` | User/Admin | Get device readings |
| POST | `/api/v1/alert-rules` | Admin | Create alert rule |
| GET | `/api/v1/alert-rules` | Admin | List alert rules |
| GET | `/api/v1/alerts` | User/Admin | List triggered alerts |
| GET | `/health` | None | Health check endpoint |

## Health Check

The API exposes a `/health` endpoint for container orchestration and monitoring:

```bash
curl http://localhost:5000/health
# Response: {"status":"healthy"}
```

In Docker, check container health status:
```bash
docker-compose ps
# Or
docker inspect --format='{{.State.Health.Status}}' sensorhub-api-1
```

## Demo Flow

### 1. Login as Admin

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"YOUR_ADMIN_EMAIL","password":"YOUR_ADMIN_PASSWORD"}'
```

Response:
```json
{"token":"eyJhbG...","expiration":"2024-01-15T12:00:00Z"}
```

### 2. Create a Device

```bash
curl -X POST http://localhost:5000/api/v1/devices \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"name":"Warehouse Sensor","location":"Building A"}'
```

### 3. Generate Device API Key

```bash
curl -X POST http://localhost:5000/api/v1/devices/{device_id}/api-key \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Response:
```json
{"apiKey":"abc123...","message":"Store this key securely. It won't be shown again."}
```

### 4. Create an Alert Rule

```bash
curl -X POST http://localhost:5000/api/v1/alert-rules \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"deviceId":"{device_id}","metricType":0,"operator":0,"threshold":30}'
```

### 5. Ingest Sensor Data

```bash
curl -X POST http://localhost:5000/api/v1/readings/ingest \
  -H "Content-Type: application/json" \
  -H "X-Device-Key: YOUR_DEVICE_API_KEY" \
  -d '{"temperature":35.5,"humidity":60}'
```

### 6. Check Alerts

```bash
curl http://localhost:5000/api/v1/alerts \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Local Development

```bash
# Copy and configure environment
cp .env.example .env
# Edit .env with your local settings

# Start PostgreSQL only
docker-compose up postgres -d

# Set environment variables for local run
export ConnectionStrings__DefaultConnection="Host=localhost;Database=sensorhub;Username=sensorhub;Password=<your-db-password>"
export Jwt__Secret="<your-secret-min-32-chars>"
export SEED_ADMIN=true
export SEED_ADMIN_EMAIL="admin@sensorhub.local"
export SEED_ADMIN_PASSWORD="<your-admin-password>"

# Run the API
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/SensorHub.Api

# Run tests
dotnet test
```

## Project Structure

```
SensorHub/
├── src/
│   └── SensorHub.Api/         # Main Web API
│       ├── Controllers/       # API endpoints
│       ├── Data/              # DbContext & migrations
│       ├── Models/            # Entities & DTOs
│       ├── Services/          # Business logic
│       └── Auth/              # API key authentication
├── tests/
│   └── SensorHub.Tests/       # Unit & integration tests
├── docker-compose.yml
├── .env.example               # Environment template
└── README.md
```

## Database Migrations

Migrations are applied automatically on app startup. To manage migrations manually:

```bash
cd src/SensorHub.Api

# Add a new migration
Jwt__Secret="dummy" dotnet ef migrations add MigrationName --output-dir Data/Migrations

# Apply migrations to database
Jwt__Secret="dummy" ConnectionStrings__DefaultConnection="Host=localhost;..." dotnet ef database update

# Or via docker-compose (migrations run on startup)
docker-compose up
```

## Tech Stack

- .NET 8 Web API
- Entity Framework Core 8 + PostgreSQL
- ASP.NET Core Identity
- JWT Bearer Authentication
- xUnit + FluentAssertions
- Docker & Docker Compose
