using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorHub.Api.Auth;
using SensorHub.Api.Models;
using SensorHub.Api.Services;
using SensorHub.Api.Validation;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class ReadingsController(DeviceService deviceService, ReadingService readingService) : ControllerBase
{
    [HttpPost("readings/ingest")]
    [Authorize(AuthenticationSchemes = DeviceApiKeyHandler.SchemeName)]
    public async Task<ActionResult<ReadingResponse>> IngestReading([FromBody] IngestReadingRequest request)
    {
        var deviceIdClaim = User.FindFirst("DeviceId")?.Value;
        if (!Guid.TryParse(deviceIdClaim, out var deviceId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication failed",
                detail: "Invalid device credentials");
        }

        var reading = await readingService.CreateReading(
            deviceId,
            request.Temperature,
            request.Humidity,
            request.Timestamp);

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

        var device = await deviceService.GetDeviceById(id);
        if (device == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Device not found",
                detail: "The specified device does not exist");
        }

        var readings = await readingService.GetReadings(id, limit, from, to);
        return Ok(readings.Select(ToResponse));
    }

    private static ReadingResponse ToResponse(Reading r) => new(
        r.Id,
        r.DeviceId,
        r.Temperature,
        r.Humidity,
        r.Timestamp);
}
