using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;

namespace SensorHub.Api.Services;

public class DeviceService(SensorHubDbContext db)
{
    public async Task<Device> CreateDevice(string name, string? location)
    {
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = location,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        db.Devices.Add(device);
        await db.SaveChangesAsync();

        return device;
    }

    public async Task<List<Device>> GetAllDevices()
    {
        return await db.Devices
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Device?> GetDeviceById(Guid id)
    {
        return await db.Devices.FindAsync(id);
    }

    public async Task<string> GenerateApiKey(Device device)
    {
        var apiKey = ApiKeyService.GenerateApiKey();
        device.ApiKeyHash = ApiKeyService.HashApiKey(apiKey);
        await db.SaveChangesAsync();
        return apiKey;
    }
}
