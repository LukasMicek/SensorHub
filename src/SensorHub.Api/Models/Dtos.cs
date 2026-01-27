using System.ComponentModel.DataAnnotations;

namespace SensorHub.Api.Models;

// Auth DTOs
public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record LoginResponse(string Token, DateTime Expiration);

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? Role = "User");

// Device DTOs
public record CreateDeviceRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(200)] string? Location);

public record DeviceResponse(
    Guid Id,
    string Name,
    string? Location,
    bool HasApiKey,
    bool IsActive,
    DateTime CreatedAt);

public record ApiKeyResponse(string ApiKey, string Message);

// Reading DTOs
public record IngestReadingRequest(
    [Range(-100, 100)] double Temperature,
    [Range(0, 100)] double Humidity,
    DateTime? Timestamp);

public record ReadingResponse(
    Guid Id,
    Guid DeviceId,
    double Temperature,
    double Humidity,
    DateTime Timestamp);

// Alert Rule DTOs
public record CreateAlertRuleRequest(
    [Required] Guid DeviceId,
    [Required] MetricType MetricType,
    [Required] ComparisonOperator Operator,
    [Required] double Threshold);

public record AlertRuleResponse(
    Guid Id,
    Guid DeviceId,
    MetricType MetricType,
    ComparisonOperator Operator,
    double Threshold,
    bool IsActive);

// Alert DTOs
public record AlertResponse(
    Guid Id,
    Guid AlertRuleId,
    Guid DeviceId,
    double Value,
    string Message,
    DateTime CreatedAt,
    bool IsAcknowledged);
