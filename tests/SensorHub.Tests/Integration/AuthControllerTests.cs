using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using Testcontainers.PostgreSql;

namespace SensorHub.Tests.Integration;

public class AuthControllerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<SensorHubDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<SensorHubDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Register_AlwaysAssignsUserRole()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "test@example.com",
            Password = "Test123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("test@example.com");
        var roles = await userManager.GetRolesAsync(user!);

        roles.Should().ContainSingle().Which.Should().Be("User");
    }

    [Fact]
    public async Task Register_IgnoresRoleFieldInRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "attacker@example.com",
            Password = "Test123!",
            Role = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("attacker@example.com");
        var roles = await userManager.GetRolesAsync(user!);

        roles.Should().ContainSingle().Which.Should().Be("User");
        roles.Should().NotContain("Admin");
    }

    [Fact]
    public async Task AssignRole_RequiresAuthentication()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/assign-role", new
        {
            UserId = Guid.NewGuid().ToString(),
            Role = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignRole_RequiresAdminRole()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "regularuser@example.com",
            Password = "Test123!"
        });

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "regularuser@example.com",
            Password = "Test123!"
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/assign-role", new
        {
            UserId = Guid.NewGuid().ToString(),
            Role = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_AdminCanAssignRoles()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "targetuser@example.com",
            Password = "Test123!"
        });

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "admin@sensorhub.local",
            Password = "Admin123!"
        });
        var adminLoginResult = await adminLoginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminLoginResult!.Token);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var targetUser = await userManager.FindByEmailAsync("targetuser@example.com");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/assign-role", new
        {
            UserId = targetUser!.Id,
            Role = "Admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedRoles = await userManager.GetRolesAsync(targetUser);
        updatedRoles.Should().ContainSingle().Which.Should().Be("Admin");
    }

    private record LoginResponse(string Token, DateTime Expiration);
}
