using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using SensorHub.Api.Validation;

namespace SensorHub.Tests.Unit;

public class QueryValidatorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(500)]
    public void ValidateLimit_ValidValues_ReturnsNull(int limit)
    {
        var result = QueryValidator.ValidateLimit(limit);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(501)]
    [InlineData(1000)]
    public void ValidateLimit_InvalidValues_ReturnsBadRequestWithProblemDetails(int limit)
    {
        var result = QueryValidator.ValidateLimit(limit);

        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result!;
        badRequest.Value.Should().BeOfType<ProblemDetails>();

        var problem = (ProblemDetails)badRequest.Value!;
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Invalid limit");
        problem.Detail.Should().Contain("Limit must be between 1 and 500");
    }

    [Fact]
    public void ValidateDateRange_FromLessThanTo_ReturnsNull()
    {
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;

        var result = QueryValidator.ValidateDateRange(from, to);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_FromEqualTo_ReturnsNull()
    {
        var date = DateTime.UtcNow;

        var result = QueryValidator.ValidateDateRange(date, date);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_FromGreaterThanTo_ReturnsBadRequestWithProblemDetails()
    {
        var from = DateTime.UtcNow.AddDays(1);
        var to = DateTime.UtcNow;

        var result = QueryValidator.ValidateDateRange(from, to);

        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = (BadRequestObjectResult)result!;
        badRequest.Value.Should().BeOfType<ProblemDetails>();

        var problem = (ProblemDetails)badRequest.Value!;
        problem.Status.Should().Be(400);
        problem.Title.Should().Be("Invalid date range");
        problem.Detail.Should().Contain("'from' must be less than or equal to 'to'");
    }

    [Fact]
    public void ValidateDateRange_NullValues_ReturnsNull()
    {
        var result = QueryValidator.ValidateDateRange(null, null);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_OnlyFromProvided_ReturnsNull()
    {
        var result = QueryValidator.ValidateDateRange(DateTime.UtcNow, null);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateDateRange_OnlyToProvided_ReturnsNull()
    {
        var result = QueryValidator.ValidateDateRange(null, DateTime.UtcNow);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateQuery_AllValid_ReturnsNull()
    {
        var result = QueryValidator.ValidateQuery(100, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateQuery_InvalidLimit_ReturnsBadRequest()
    {
        var result = QueryValidator.ValidateQuery(0, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void ValidateQuery_InvalidDateRange_ReturnsBadRequest()
    {
        var result = QueryValidator.ValidateQuery(100, DateTime.UtcNow.AddDays(1), DateTime.UtcNow);
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
