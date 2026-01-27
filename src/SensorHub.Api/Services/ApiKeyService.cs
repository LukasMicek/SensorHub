using System.Security.Cryptography;
using System.Text;

namespace SensorHub.Api.Services;

// Handles API key generation and validation for device authentication.
// We store only the hash of the API key in the database for security -
// if the database is compromised, attackers can't use the hashed keys.
public static class ApiKeyService
{
    // Generates a cryptographically secure random API key.
    // Uses URL-safe Base64 encoding (replaces +/ with -_) so it can be safely used in headers.
    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        // Convert to URL-safe Base64: replace + with -, / with _, and remove padding =
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    // Creates a SHA256 hash of the API key for secure storage.
    // SHA256 is a one-way function - you can't reverse the hash to get the original key.
    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes);
    }

    // Validates an API key by hashing it and comparing to the stored hash.
    // Case-insensitive comparison since hex strings can vary in case.
    public static bool ValidateApiKey(string providedKey, string storedHash)
    {
        var providedHash = HashApiKey(providedKey);
        return string.Equals(providedHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
