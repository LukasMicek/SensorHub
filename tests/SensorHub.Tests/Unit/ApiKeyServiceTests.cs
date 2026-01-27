using FluentAssertions;
using SensorHub.Api.Services;

namespace SensorHub.Tests.Unit;

public class ApiKeyServiceTests
{
    [Fact]
    public void GenerateApiKey_ShouldReturnNonEmptyString()
    {
        var apiKey = ApiKeyService.GenerateApiKey();

        apiKey.Should().NotBeNullOrWhiteSpace();
        apiKey.Length.Should().BeGreaterThan(20);
    }

    [Fact]
    public void HashApiKey_ShouldReturnConsistentHash()
    {
        var apiKey = "test-api-key-12345";

        var hash1 = ApiKeyService.HashApiKey(apiKey);
        var hash2 = ApiKeyService.HashApiKey(apiKey);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ValidateApiKey_ShouldReturnTrue_ForValidKey()
    {
        var apiKey = ApiKeyService.GenerateApiKey();
        var hash = ApiKeyService.HashApiKey(apiKey);

        var result = ApiKeyService.ValidateApiKey(apiKey, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateApiKey_ShouldReturnFalse_ForInvalidKey()
    {
        var apiKey = ApiKeyService.GenerateApiKey();
        var hash = ApiKeyService.HashApiKey(apiKey);

        var result = ApiKeyService.ValidateApiKey("wrong-key", hash);

        result.Should().BeFalse();
    }
}
