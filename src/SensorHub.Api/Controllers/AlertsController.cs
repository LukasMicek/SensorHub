using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize(Roles = "Admin,User")]
public class AlertsController(SensorHubDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AlertResponse>>> GetAlerts(
        [FromQuery] Guid? deviceId = null,
        [FromQuery] bool? acknowledged = null,
        [FromQuery] int limit = 100)
    {
        var query = db.Alerts.AsQueryable();

        if (deviceId.HasValue)
            query = query.Where(a => a.DeviceId == deviceId.Value);
        if (acknowledged.HasValue)
            query = query.Where(a => a.IsAcknowledged == acknowledged.Value);

        var alerts = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return Ok(alerts.Select(ToResponse));
    }

    private static AlertResponse ToResponse(Alert a) => new(
        a.Id,
        a.AlertRuleId,
        a.DeviceId,
        a.Value,
        a.Message,
        a.CreatedAt,
        a.IsAcknowledged);
}
