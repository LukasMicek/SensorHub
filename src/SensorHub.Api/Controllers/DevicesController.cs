using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1/devices")]
[Authorize]
public class DevicesController(DeviceService deviceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DeviceResponse>> CreateDevice([FromBody] CreateDeviceRequest request)
    {
        var device = await deviceService.CreateDevice(request.Name, request.Location);
        return CreatedAtAction(nameof(GetDevices), new { id = device.Id }, ToResponse(device));
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceResponse>>> GetDevices()
    {
        var devices = await deviceService.GetAllDevices();
        return Ok(devices.Select(ToResponse));
    }

    [HttpPost("{id:guid}/api-key")]
    public async Task<ActionResult<ApiKeyResponse>> GenerateApiKey(Guid id)
    {
        var device = await deviceService.GetDeviceById(id);
        if (device == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Device not found",
                detail: "The specified device does not exist");
        }

        var apiKey = await deviceService.GenerateApiKey(device);
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
