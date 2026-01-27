# SensorHub

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

# Start the application
docker-compose up --build

# API is available at http://localhost:5000
# Swagger UI at http://localhost:5000/swagger
```

## Default Admin Credentials

- **Email**: `admin@sensorhub.local`
- **Password**: `Admin123!`

## API Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/v1/auth/login` | None | Login and get JWT token |
| POST | `/api/v1/auth/register` | None | Register new user |
| POST | `/api/v1/devices` | Admin | Create a device |
| GET | `/api/v1/devices` | Admin | List all devices |
| POST | `/api/v1/devices/{id}/api-key` | Admin | Generate device API key |
| POST | `/api/v1/readings/ingest` | Device Key | Ingest sensor reading |
| GET | `/api/v1/devices/{id}/readings` | User/Admin | Get device readings |
| POST | `/api/v1/alert-rules` | Admin | Create alert rule |
| GET | `/api/v1/alert-rules` | Admin | List alert rules |
| GET | `/api/v1/alerts` | User/Admin | List triggered alerts |

## Demo Flow

### 1. Login as Admin

```bash
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sensorhub.local","password":"Admin123!"}'
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
# Start PostgreSQL
docker-compose up postgres -d

# Run the API
dotnet run --project src/SensorHub.Api

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
└── README.md
```

## Configuration

Environment variables (set in docker-compose.yml or appsettings.json):

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (min 32 chars) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |

## Tech Stack

- .NET 8 Web API
- Entity Framework Core 8 + PostgreSQL
- ASP.NET Core Identity
- JWT Bearer Authentication
- xUnit + FluentAssertions
- Docker & Docker Compose
