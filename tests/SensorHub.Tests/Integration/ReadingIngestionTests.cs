using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using Testcontainers.PostgreSql;

namespace SensorHub.Tests.Integration;

public class ReadingIngestionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new TestWebApplicationFactory(_postgres.GetConnectionString());
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<string> GetAdminToken()
    {
        const string adminEmail = "admin@test.com";
        const string adminPassword = "AdminTest123!";

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
        await userManager.CreateAsync(admin, adminPassword);
        await userManager.AddToRoleAsync(admin, "Admin");

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = adminEmail,
            Password = adminPassword
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }

    [Fact]
    public async Task IngestReading_WithValidApiKey_CreatesReading()
    {
        // Arrange - Create device and get API key
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDeviceResponse = await _client.PostAsJsonAsync("/api/v1/devices", new
        {
            Name = "Test Sensor",
            Location = "Building A"
        });
        createDeviceResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var device = await createDeviceResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        var apiKeyResponse = await _client.PostAsync($"/api/v1/devices/{device!.Id}/api-key", null);
        apiKeyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var apiKeyResult = await apiKeyResponse.Content.ReadFromJsonAsync<ApiKeyResponse>();

        // Act - Ingest reading using device API key
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Add("X-Device-Key", apiKeyResult!.ApiKey);

        var ingestResponse = await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 25.5,
            Humidity = 60.0
        });

        // Assert
        ingestResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reading = await ingestResponse.Content.ReadFromJsonAsync<ReadingResponse>();
        reading.Should().NotBeNull();
        reading!.DeviceId.Should().Be(device.Id);
        reading.Temperature.Should().Be(25.5);
        reading.Humidity.Should().Be(60.0);
    }

    [Fact]
    public async Task IngestReading_PersistsToDatabase()
    {
        // Arrange
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDeviceResponse = await _client.PostAsJsonAsync("/api/v1/devices", new
        {
            Name = "Persistence Test Sensor"
        });
        var device = await createDeviceResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        var apiKeyResponse = await _client.PostAsync($"/api/v1/devices/{device!.Id}/api-key", null);
        var apiKeyResult = await apiKeyResponse.Content.ReadFromJsonAsync<ApiKeyResponse>();

        // Act - Ingest reading
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Add("X-Device-Key", apiKeyResult!.ApiKey);

        await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 22.0,
            Humidity = 55.0
        });

        // Assert - Verify persistence via GET endpoint
        _client.DefaultRequestHeaders.Remove("X-Device-Key");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var getReadingsResponse = await _client.GetAsync($"/api/v1/devices/{device.Id}/readings");
        getReadingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var readings = await getReadingsResponse.Content.ReadFromJsonAsync<List<ReadingResponse>>();

        readings.Should().HaveCount(1);
        readings![0].Temperature.Should().Be(22.0);
        readings[0].Humidity.Should().Be(55.0);
    }

    [Fact]
    public async Task IngestReading_WithoutApiKey_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 25.5,
            Humidity = 60.0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IngestReading_WithInvalidApiKey_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("X-Device-Key", "invalid-api-key");

        var response = await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 25.5,
            Humidity = 60.0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task IngestReading_TriggersAlertWhenThresholdBreached()
    {
        // Arrange - Create device with alert rule
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDeviceResponse = await _client.PostAsJsonAsync("/api/v1/devices", new
        {
            Name = "Alert Test Sensor"
        });
        var device = await createDeviceResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        // Create alert rule: Temperature > 30
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();
            db.AlertRules.Add(new AlertRule
            {
                Id = Guid.NewGuid(),
                DeviceId = device!.Id,
                MetricType = MetricType.Temperature,
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 30.0,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var apiKeyResponse = await _client.PostAsync($"/api/v1/devices/{device!.Id}/api-key", null);
        var apiKeyResult = await apiKeyResponse.Content.ReadFromJsonAsync<ApiKeyResponse>();

        // Act - Ingest reading that exceeds threshold
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Add("X-Device-Key", apiKeyResult!.ApiKey);

        await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 35.0,
            Humidity = 50.0
        });

        // Assert - Verify alert was created
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();
            var alerts = db.Alerts.Where(a => a.DeviceId == device.Id).ToList();

            alerts.Should().HaveCount(1);
            alerts[0].Value.Should().Be(35.0);
            alerts[0].Message.Should().Contain("Temperature");
            alerts[0].Message.Should().Contain("35");
            alerts[0].Message.Should().Contain("30");
        }
    }

    [Fact]
    public async Task IngestReading_DoesNotTriggerAlertWhenBelowThreshold()
    {
        // Arrange
        var adminToken = await GetAdminToken();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var createDeviceResponse = await _client.PostAsJsonAsync("/api/v1/devices", new
        {
            Name = "No Alert Sensor"
        });
        var device = await createDeviceResponse.Content.ReadFromJsonAsync<DeviceResponse>();

        // Create alert rule: Temperature > 30
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();
            db.AlertRules.Add(new AlertRule
            {
                Id = Guid.NewGuid(),
                DeviceId = device!.Id,
                MetricType = MetricType.Temperature,
                Operator = ComparisonOperator.GreaterThan,
                Threshold = 30.0,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var apiKeyResponse = await _client.PostAsync($"/api/v1/devices/{device!.Id}/api-key", null);
        var apiKeyResult = await apiKeyResponse.Content.ReadFromJsonAsync<ApiKeyResponse>();

        // Act - Ingest reading below threshold
        _client.DefaultRequestHeaders.Authorization = null;
        _client.DefaultRequestHeaders.Add("X-Device-Key", apiKeyResult!.ApiKey);

        await _client.PostAsJsonAsync("/api/v1/readings/ingest", new
        {
            Temperature = 25.0,
            Humidity = 50.0
        });

        // Assert - Verify no alert was created
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();
            var alerts = db.Alerts.Where(a => a.DeviceId == device!.Id).ToList();
            alerts.Should().BeEmpty();
        }
    }

    private record LoginResponse(string Token, DateTime Expiration);
    private record DeviceResponse(Guid Id, string Name, string? Location, bool HasApiKey, bool IsActive, DateTime CreatedAt);
    private record ApiKeyResponse(string ApiKey, string Message);
    private record ReadingResponse(Guid Id, Guid DeviceId, double Temperature, double Humidity, DateTime Timestamp);
}
