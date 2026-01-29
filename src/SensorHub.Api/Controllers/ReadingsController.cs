using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Auth;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using SensorHub.Api.Services;
using SensorHub.Api.Validation;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ReadingsController(SensorHubDbContext db, AlertService alertService) : ControllerBase
{
    [HttpPost("readings/ingest")]
    [Authorize(AuthenticationSchemes = DeviceApiKeyHandler.SchemeName)]
    public async Task<ActionResult<ReadingResponse>> IngestReading([FromBody] IngestReadingRequest request)
    {
        var deviceIdClaim = User.FindFirst("DeviceId")?.Value;
        if (!Guid.TryParse(deviceIdClaim, out var deviceId))
        {
            return Unauthorized(new { message = "Invalid device" });
        }

        var reading = new Reading
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            Temperature = request.Temperature,
            Humidity = request.Humidity,
            Timestamp = request.Timestamp ?? DateTime.UtcNow
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        await alertService.EvaluateAndCreateAlerts(reading);

        return Ok(ToResponse(reading));
    }

    [HttpGet("devices/{id:guid}/readings")]
    [Authorize(Roles = "Admin,User")]
    public async Task<ActionResult<List<ReadingResponse>>> GetReadings(
        Guid id,
        [FromQuery] int limit = 100,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var validationError = QueryValidator.ValidateQuery(limit, from, to);
        if (validationError != null)
            return validationError;

        var device = await db.Devices.FindAsync(id);
        if (device == null)
        {
            return NotFound(new { message = "Device not found" });
        }

        var query = db.Readings.Where(r => r.DeviceId == id);

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        var readings = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();

        return Ok(readings.Select(ToResponse));
    }

    private static ReadingResponse ToResponse(Reading r) => new(
        r.Id,
        r.DeviceId,
        r.Temperature,
        r.Humidity,
        r.Timestamp);
}
