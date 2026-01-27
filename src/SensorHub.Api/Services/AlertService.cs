using SensorHub.Api.Data;
using SensorHub.Api.Models;

namespace SensorHub.Api.Services;

// Evaluates sensor readings against alert rules and creates alerts when thresholds are breached.
// This is the core business logic for the alerting system.
public class AlertService(SensorHubDbContext db)
{
    // Checks all active alert rules for a device and creates alerts for any breached thresholds.
    // Called automatically when a new reading is ingested.
    public async Task<List<Alert>> EvaluateAndCreateAlerts(Reading reading, CancellationToken ct = default)
    {
        var alerts = new List<Alert>();

        // Get all active rules for this specific device
        var rules = db.AlertRules
            .Where(r => r.DeviceId == reading.DeviceId && r.IsActive)
            .ToList();

        foreach (var rule in rules)
        {
            // Pick the correct metric value based on what the rule is monitoring
            var value = rule.MetricType == MetricType.Temperature
                ? reading.Temperature
                : reading.Humidity;

            // Check if the reading value breaches the rule's threshold
            if (EvaluateRule(rule, value))
            {
                var alert = new Alert
                {
                    Id = Guid.NewGuid(),
                    AlertRuleId = rule.Id,
                    DeviceId = reading.DeviceId,
                    Value = value,
                    Message = $"{rule.MetricType} value {value} {GetOperatorSymbol(rule.Operator)} {rule.Threshold}",
                    CreatedAt = DateTime.UtcNow
                };
                alerts.Add(alert);
                db.Alerts.Add(alert);
            }
        }

        // Only save to database if we created any alerts (avoid unnecessary DB calls)
        if (alerts.Count > 0)
            await db.SaveChangesAsync(ct);

        return alerts;
    }

    // Compares a value against a rule's threshold using the specified operator.
    // Made static and public so it can be easily unit tested without database dependencies.
    public static bool EvaluateRule(AlertRule rule, double value)
    {
        return rule.Operator switch
        {
            ComparisonOperator.GreaterThan => value > rule.Threshold,
            ComparisonOperator.LessThan => value < rule.Threshold,
            ComparisonOperator.GreaterThanOrEqual => value >= rule.Threshold,
            ComparisonOperator.LessThanOrEqual => value <= rule.Threshold,
            // For equality, use a small tolerance to handle floating-point precision issues
            ComparisonOperator.Equal => Math.Abs(value - rule.Threshold) < 0.0001,
            _ => false
        };
    }

    // Converts enum to human-readable symbol for alert messages.
    private static string GetOperatorSymbol(ComparisonOperator op) => op switch
    {
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.LessThanOrEqual => "<=",
        ComparisonOperator.Equal => "==",
        _ => "?"
    };
}
