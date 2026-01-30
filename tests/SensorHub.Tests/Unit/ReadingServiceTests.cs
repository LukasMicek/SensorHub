using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

namespace SensorHub.Tests.Unit;

public class ReadingServiceTests : IDisposable
{
    private readonly SensorHubDbContext _db;
    private readonly ReadingService _service;
    private readonly Guid _deviceId;

    public ReadingServiceTests()
    {
        var options = new DbContextOptionsBuilder<SensorHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new SensorHubDbContext(options);
        var alertService = new AlertService(_db);
        _service = new ReadingService(_db, alertService);

        _deviceId = Guid.NewGuid();
        _db.Devices.Add(new Device
        {
            Id = _deviceId,
            Name = "Test Device",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task CreateReading_SetsCorrectProperties()
    {
        var reading = await _service.CreateReading(_deviceId, 25.5, 60.0, null);

        reading.Id.Should().NotBeEmpty();
        reading.DeviceId.Should().Be(_deviceId);
        reading.Temperature.Should().Be(25.5);
        reading.Humidity.Should().Be(60.0);
        reading.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateReading_WithTimestamp_UsesProvidedTimestamp()
    {
        var timestamp = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var reading = await _service.CreateReading(_deviceId, 20.0, 50.0, timestamp);

        reading.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task CreateReading_PersistsToDatabase()
    {
        var reading = await _service.CreateReading(_deviceId, 22.0, 55.0, null);

        var fromDb = await _db.Readings.FindAsync(reading.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Temperature.Should().Be(22.0);
    }

    [Fact]
    public async Task GetReadings_ReturnsOrderedByTimestampDescending()
    {
        var t1 = DateTime.UtcNow.AddHours(-3);
        var t2 = DateTime.UtcNow.AddHours(-2);
        var t3 = DateTime.UtcNow.AddHours(-1);

        await _service.CreateReading(_deviceId, 20.0, 50.0, t1);
        await _service.CreateReading(_deviceId, 21.0, 51.0, t2);
        await _service.CreateReading(_deviceId, 22.0, 52.0, t3);

        var readings = await _service.GetReadings(_deviceId, 100, null, null);

        readings.Should().HaveCount(3);
        readings[0].Timestamp.Should().Be(t3);
        readings[1].Timestamp.Should().Be(t2);
        readings[2].Timestamp.Should().Be(t1);
    }

    [Fact]
    public async Task GetReadings_RespectsLimit()
    {
        for (int i = 0; i < 10; i++)
        {
            await _service.CreateReading(_deviceId, 20.0 + i, 50.0, null);
        }

        var readings = await _service.GetReadings(_deviceId, 5, null, null);

        readings.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetReadings_FiltersFromDate()
    {
        var old = DateTime.UtcNow.AddDays(-10);
        var recent = DateTime.UtcNow.AddDays(-1);

        await _service.CreateReading(_deviceId, 20.0, 50.0, old);
        await _service.CreateReading(_deviceId, 21.0, 51.0, recent);

        var readings = await _service.GetReadings(_deviceId, 100, DateTime.UtcNow.AddDays(-5), null);

        readings.Should().HaveCount(1);
        readings[0].Temperature.Should().Be(21.0);
    }

    [Fact]
    public async Task GetReadings_FiltersToDate()
    {
        var old = DateTime.UtcNow.AddDays(-10);
        var recent = DateTime.UtcNow.AddDays(-1);

        await _service.CreateReading(_deviceId, 20.0, 50.0, old);
        await _service.CreateReading(_deviceId, 21.0, 51.0, recent);

        var readings = await _service.GetReadings(_deviceId, 100, null, DateTime.UtcNow.AddDays(-5));

        readings.Should().HaveCount(1);
        readings[0].Temperature.Should().Be(20.0);
    }

    [Fact]
    public async Task GetReadings_FiltersByDeviceId()
    {
        var otherDeviceId = Guid.NewGuid();
        _db.Devices.Add(new Device { Id = otherDeviceId, Name = "Other", IsActive = true, CreatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        await _service.CreateReading(_deviceId, 20.0, 50.0, null);
        await _service.CreateReading(otherDeviceId, 30.0, 60.0, null);

        var readings = await _service.GetReadings(_deviceId, 100, null, null);

        readings.Should().HaveCount(1);
        readings[0].DeviceId.Should().Be(_deviceId);
    }
}
