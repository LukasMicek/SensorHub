using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Testcontainers.PostgreSql;

namespace SensorHub.Tests.Integration;

public class AuthFlowTests : IAsyncLifetime
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

    [Fact]
    public async Task FullAuthFlow_RegisterLoginAndAccessProtectedEndpoint()
    {
        // Step 1: Register a new user
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        registerResult.Should().NotBeNull();
        registerResult!.Message.Should().NotBeNullOrEmpty();

        // Step 2: Login with the registered credentials
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginResult.Should().NotBeNull();
        loginResult!.Token.Should().NotBeNullOrEmpty();

        // Step 3: Access protected endpoint with token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResult.Token);

        // User role can access readings endpoint (but needs a device first - let's check we get proper response)
        var readingsResponse = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}/readings");

        // Should be NotFound (not unauthorized) because user is authenticated but device doesn't exist
        readingsResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // First register a user
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "validuser@example.com",
            Password = "ValidPass123!"
        });

        // Try to login with wrong password
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "validuser@example.com",
            Password = "WrongPassword123!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword123!"
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}/readings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        var response = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}/readings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "weak@example.com",
            Password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Register first user
        await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "duplicate@example.com",
            Password = "ValidPass123!"
        });

        // Try to register with same email
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "duplicate@example.com",
            Password = "AnotherPass123!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private record RegisterResponse(string Message);
    private record LoginResponse(string Token, DateTime Expiration);
}
