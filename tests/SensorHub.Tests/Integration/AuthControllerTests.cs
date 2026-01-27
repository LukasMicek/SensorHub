using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using SensorHub.Api.Models;
using Testcontainers.PostgreSql;

namespace SensorHub.Tests.Integration;

public class AuthControllerTests : IAsyncLifetime
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

    private async Task<ApplicationUser> CreateTestAdmin(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
            await roleManager.CreateAsync(new IdentityRole("Admin"));

        var admin = new ApplicationUser { UserName = email, Email = email };
        await userManager.CreateAsync(admin, password);
        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
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
        const string adminEmail = "testadmin@example.com";
        const string adminPassword = "AdminTest123!";
        await CreateTestAdmin(adminEmail, adminPassword);

        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "targetuser@example.com",
            Password = "Test123!"
        });

        var adminLoginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = adminEmail,
            Password = adminPassword
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
