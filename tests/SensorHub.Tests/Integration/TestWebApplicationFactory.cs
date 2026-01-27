using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SensorHub.Api.Data;

namespace SensorHub.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "TestSecretKeyForIntegrationTestsMin32Chars!",
                ["Jwt:Issuer"] = "SensorHub",
                ["Jwt:Audience"] = "SensorHub",
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<SensorHubDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<SensorHubDbContext>(options =>
                options.UseNpgsql(_connectionString));
        });
    }
}
