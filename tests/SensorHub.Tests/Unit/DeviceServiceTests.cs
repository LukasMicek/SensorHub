using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SensorHub.Api.Data;
using SensorHub.Api.Services;

namespace SensorHub.Tests.Unit;

public class DeviceServiceTests : IDisposable
{
    private readonly SensorHubDbContext _db;
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        var options = new DbContextOptionsBuilder<SensorHubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new SensorHubDbContext(options);
        _service = new DeviceService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task CreateDevice_SetsCorrectProperties()
    {
        var device = await _service.CreateDevice("Test Sensor", "Building A");

        device.Id.Should().NotBeEmpty();
        device.Name.Should().Be("Test Sensor");
        device.Location.Should().Be("Building A");
        device.IsActive.Should().BeTrue();
        device.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateDevice_PersistsToDatabase()
    {
        var device = await _service.CreateDevice("Persisted Sensor", null);

        var fromDb = await _db.Devices.FindAsync(device.Id);
        fromDb.Should().NotBeNull();
        fromDb!.Name.Should().Be("Persisted Sensor");
    }

    [Fact]
    public async Task GetAllDevices_ReturnsOrderedByCreatedAtDescending()
    {
        await _service.CreateDevice("Device 1", null);
        await Task.Delay(10);
        await _service.CreateDevice("Device 2", null);
        await Task.Delay(10);
        await _service.CreateDevice("Device 3", null);

        var devices = await _service.GetAllDevices();

        devices.Should().HaveCount(3);
        devices[0].Name.Should().Be("Device 3");
        devices[1].Name.Should().Be("Device 2");
        devices[2].Name.Should().Be("Device 1");
    }

    [Fact]
    public async Task GetDeviceById_ExistingDevice_ReturnsDevice()
    {
        var created = await _service.CreateDevice("Findable", null);

        var found = await _service.GetDeviceById(created.Id);

        found.Should().NotBeNull();
        found!.Name.Should().Be("Findable");
    }

    [Fact]
    public async Task GetDeviceById_NonExistent_ReturnsNull()
    {
        var found = await _service.GetDeviceById(Guid.NewGuid());
        found.Should().BeNull();
    }

    [Fact]
    public async Task GenerateApiKey_SetsApiKeyHash()
    {
        var device = await _service.CreateDevice("Key Test", null);
        device.ApiKeyHash.Should().BeNull();

        var apiKey = await _service.GenerateApiKey(device);

        apiKey.Should().NotBeNullOrEmpty();
        device.ApiKeyHash.Should().NotBeNullOrEmpty();
        ApiKeyService.ValidateApiKey(apiKey, device.ApiKeyHash!).Should().BeTrue();
    }
}
