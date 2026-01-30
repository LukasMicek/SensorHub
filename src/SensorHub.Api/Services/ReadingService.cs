using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;

namespace SensorHub.Api.Services;

public class ReadingService(SensorHubDbContext db, AlertService alertService)
{
    public async Task<Reading> CreateReading(Guid deviceId, double temperature, double humidity, DateTime? timestamp)
    {
        var reading = new Reading
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            Temperature = temperature,
            Humidity = humidity,
            Timestamp = timestamp ?? DateTime.UtcNow
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        await alertService.EvaluateAndCreateAlerts(reading);

        return reading;
    }

    public async Task<List<Reading>> GetReadings(Guid deviceId, int limit, DateTime? from, DateTime? to)
    {
        var query = db.Readings.Where(r => r.DeviceId == deviceId);

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        return await query
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync();
    }
}
