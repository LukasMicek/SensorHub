using FluentAssertions;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

namespace SensorHub.Tests.Unit;

public class AlertServiceTests
{
    [Theory]
    [InlineData(ComparisonOperator.GreaterThan, 25.0, 30.0, true)]
    [InlineData(ComparisonOperator.GreaterThan, 25.0, 20.0, false)]
    [InlineData(ComparisonOperator.GreaterThan, 25.0, 25.0, false)]
    [InlineData(ComparisonOperator.LessThan, 25.0, 20.0, true)]
    [InlineData(ComparisonOperator.LessThan, 25.0, 30.0, false)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 25.0, 25.0, true)]
    [InlineData(ComparisonOperator.GreaterThanOrEqual, 25.0, 26.0, true)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 25.0, 25.0, true)]
    [InlineData(ComparisonOperator.LessThanOrEqual, 25.0, 24.0, true)]
    [InlineData(ComparisonOperator.Equal, 25.0, 25.0, true)]
    [InlineData(ComparisonOperator.Equal, 25.0, 25.001, false)]
    public void EvaluateRule_ShouldCorrectlyCompareValues(
        ComparisonOperator op, double threshold, double value, bool expected)
    {
        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            DeviceId = Guid.NewGuid(),
            MetricType = MetricType.Temperature,
            Operator = op,
            Threshold = threshold,
            IsActive = true
        };

        var result = AlertService.EvaluateRule(rule, value);

        result.Should().Be(expected);
    }
}
