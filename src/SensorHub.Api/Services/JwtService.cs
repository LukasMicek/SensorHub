using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SensorHub.Api.Models;

namespace SensorHub.Api.Services;

// Generates JWT (JSON Web Tokens) for authenticated users.
public class JwtService(IConfiguration config)
{
    // Creates a signed JWT token containing user identity and role claims.
    // The token is valid for a configured number of hours (default: 1 hour).
    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        // Load JWT settings from configuration (appsettings.json or environment variables)
        var secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
        var issuer = config["Jwt:Issuer"] ?? "SensorHub";
        var audience = config["Jwt:Audience"] ?? "SensorHub";
        var expirationHours = int.Parse(config["Jwt:ExpirationHours"] ?? "1");

        // Create signing credentials using HMAC-SHA256 algorithm
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims are pieces of information about the user embedded in the token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),       // User's unique ID
            new(ClaimTypes.Email, user.Email ?? ""),        // User's email
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())  // Unique token ID
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Build and sign the token
        var token = new JwtSecurityToken(
            issuer: issuer,       // Who created the token
            audience: audience,   // Who the token is intended for
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials);

        // Serialize the token to a string that can be sent to the client
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
