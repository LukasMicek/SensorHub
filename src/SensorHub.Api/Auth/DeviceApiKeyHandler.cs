using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SensorHub.Api.Data;
using SensorHub.Api.Services;

namespace SensorHub.Api.Auth;

// Custom authentication handler for device API keys.
// This allows IoT devices to authenticate using a simple API key in the header
// instead of JWT tokens 
//
// Usage: Add [Authorize(AuthenticationSchemes = "DeviceApiKey")] to endpoints
// that should accept device API key authentication.
public class DeviceApiKeyHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly SensorHubDbContext _db;

    // Constants for the authentication scheme - used to reference this handler
    public const string SchemeName = "DeviceApiKey";
    public const string HeaderName = "X-Device-Key";

    public DeviceApiKeyHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        SensorHubDbContext db) : base(options, logger, encoder)
    {
        _db = db;
    }

    // Called by ASP.NET Core when a request comes in to an endpoint requiring this auth scheme.
    // Validates the API key and creates a ClaimsPrincipal representing the authenticated device.
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the X-Device-Key header exists
        if (!Request.Headers.TryGetValue(HeaderName, out var apiKeyHeader))
        {
            return AuthenticateResult.NoResult();  // No API key provided, let other auth handlers try
        }

        var apiKey = apiKeyHeader.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.Fail("API key is empty");
        }

        // Hash the provided key and look for a matching device in the database
        var apiKeyHash = ApiKeyService.HashApiKey(apiKey);
        var device = await _db.Devices
            .FirstOrDefaultAsync(d => d.ApiKeyHash == apiKeyHash && d.IsActive);

        if (device == null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        // Create claims that identify this device - these can be accessed in controllers
        // via User.FindFirst("DeviceId") to know which device made the request
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, device.Id.ToString()),
            new Claim("DeviceId", device.Id.ToString()),
            new Claim("DeviceName", device.Name)
        };

        // Build the authentication ticket that ASP.NET Core uses to track the authenticated entity
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
