using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using Testcontainers.PostgreSql;

namespace SensorHub.Tests.Integration;

public class QueryValidationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private TestWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _testDeviceId;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new TestWebApplicationFactory(_postgres.GetConnectionString());
        _client = _factory.CreateClient();

        await CreateTestUserAndLogin();
        await CreateTestDevice();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task CreateTestUserAndLogin()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("User"))
            await roleManager.CreateAsync(new IdentityRole("User"));

        var user = new ApplicationUser { UserName = "testuser@example.com", Email = "testuser@example.com" };
        await userManager.CreateAsync(user, "Test123!");
        await userManager.AddToRoleAsync(user, "User");

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "testuser@example.com",
            Password = "Test123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }

    private async Task CreateTestDevice()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();

        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = "Test Device",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        _testDeviceId = device.Id;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetReadings_InvalidLimit_Zero_Or_Negative_Returns400(int limit)
    {
        var response = await _client.GetAsync($"/api/v1/devices/{_testDeviceId}/readings?limit={limit}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Detail.Should().Contain("Limit must be between 1 and 500");
    }

    [Fact]
    public async Task GetReadings_InvalidLimit_ExceedsMax_Returns400()
    {
        var response = await _client.GetAsync($"/api/v1/devices/{_testDeviceId}/readings?limit=501");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Detail.Should().Contain("Limit must be between 1 and 500");
    }

    [Fact]
    public async Task GetReadings_ValidLimit_Returns200()
    {
        var response = await _client.GetAsync($"/api/v1/devices/{_testDeviceId}/readings?limit=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetReadings_FromGreaterThanTo_Returns400()
    {
        var from = DateTime.UtcNow.AddDays(1).ToString("O");
        var to = DateTime.UtcNow.ToString("O");

        var response = await _client.GetAsync($"/api/v1/devices/{_testDeviceId}/readings?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Detail.Should().Contain("'from' must be less than or equal to 'to'");
    }

    [Fact]
    public async Task GetReadings_ValidDateRange_Returns200()
    {
        var from = DateTime.UtcNow.AddDays(-1).ToString("O");
        var to = DateTime.UtcNow.ToString("O");

        var response = await _client.GetAsync($"/api/v1/devices/{_testDeviceId}/readings?from={from}&to={to}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAlerts_InvalidLimit_Returns400(int limit)
    {
        var response = await _client.GetAsync($"/api/v1/alerts?limit={limit}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Detail.Should().Contain("Limit must be between 1 and 500");
    }

    [Fact]
    public async Task GetAlerts_LimitExceedsMax_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/alerts?limit=501");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>();
        problem!.Detail.Should().Contain("Limit must be between 1 and 500");
    }

    [Fact]
    public async Task GetAlerts_ValidLimit_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/alerts?limit=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private record LoginResponse(string Token, DateTime Expiration);
    private record ProblemDetailsResponse(int? Status, string? Title, string? Detail);
}
