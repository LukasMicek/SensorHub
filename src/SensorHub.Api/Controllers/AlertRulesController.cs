using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1/alert-rules")]
[Authorize(Roles = "Admin")]
public class AlertRulesController(SensorHubDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AlertRuleResponse>> CreateAlertRule([FromBody] CreateAlertRuleRequest request)
    {
        var device = await db.Devices.FindAsync(request.DeviceId);
        if (device == null)
        {
            return NotFound(new { message = "Device not found" });
        }

        var rule = new AlertRule
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            MetricType = request.MetricType,
            Operator = request.Operator,
            Threshold = request.Threshold,
            IsActive = true
        };

        db.AlertRules.Add(rule);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAlertRules), new { id = rule.Id }, ToResponse(rule));
    }

    [HttpGet]
    public async Task<ActionResult<List<AlertRuleResponse>>> GetAlertRules()
    {
        var rules = await db.AlertRules.ToListAsync();
        return Ok(rules.Select(ToResponse));
    }

    private static AlertRuleResponse ToResponse(AlertRule r) => new(
        r.Id,
        r.DeviceId,
        r.MetricType,
        r.Operator,
        r.Threshold,
        r.IsActive);
}
