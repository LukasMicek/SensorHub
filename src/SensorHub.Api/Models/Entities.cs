using Microsoft.AspNetCore.Identity;

namespace SensorHub.Api.Models;

public class ApplicationUser : IdentityUser
{
}

public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ApiKeyHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
    public ICollection<AlertRule> AlertRules { get; set; } = new List<AlertRule>();
}

public class Reading
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Device Device { get; set; } = null!;
}

public class AlertRule
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public MetricType MetricType { get; set; }
    public ComparisonOperator Operator { get; set; }
    public double Threshold { get; set; }
    public bool IsActive { get; set; } = true;

    public Device Device { get; set; } = null!;
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}

public class Alert
{
    public Guid Id { get; set; }
    public Guid AlertRuleId { get; set; }
    public Guid DeviceId { get; set; }
    public double Value { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }

    public AlertRule AlertRule { get; set; } = null!;
    public Device Device { get; set; } = null!;
}

public enum MetricType
{
    Temperature,
    Humidity
}

public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Equal
}
