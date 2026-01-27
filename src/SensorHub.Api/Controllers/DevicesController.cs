using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize(Roles = "Admin")]
public class DevicesController(SensorHubDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> CreateDevice([FromBody] CreateDeviceRequest request)
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Location = request.Location,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDevices), new { id = device.Id }, ToResponse(device));
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceResponse>>> GetDevices()
    {
        var devices = await db.Devices
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return Ok(devices.Select(ToResponse));
    }

    [HttpPost("{id:guid}/api-key")]
    public async Task<ActionResult<ApiKeyResponse>> GenerateApiKey(Guid id)
    {
        var device = await db.Devices.FindAsync(id);
        if (device == null)
        {
            return NotFound(new { message = "Device not found" });
        }

        var apiKey = ApiKeyService.GenerateApiKey();
        device.ApiKeyHash = ApiKeyService.HashApiKey(apiKey);
        await db.SaveChangesAsync();

        return Ok(new ApiKeyResponse(apiKey, "Store this key securely. It won't be shown again."));
    }

    private static DeviceResponse ToResponse(Device d) => new(
        d.Id,
        d.Name,
        d.Location,
        !string.IsNullOrEmpty(d.ApiKeyHash),
        d.IsActive,
        d.CreatedAt);
}
