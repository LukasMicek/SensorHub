using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SensorHub.Api.Auth;
using SensorHub.Api.Data;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Database: Configure Entity Framework Core to use PostgreSQL
// The connection string comes from appsettings.json or environment variables
builder.Services.AddDbContext<SensorHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity: ASP.NET Core's built-in user management system
// Handles password hashing, user storage, and role management
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password requirements - balance security with usability
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    })
    .AddEntityFrameworkStores<SensorHubDbContext>()  // Store users in PostgreSQL database
    .AddDefaultTokenProviders();  // For password reset tokens, etc.

// Authentication: Configure how users prove their identity
// 1. JWT Bearer tokens - for web/mobile app users (Admin, User roles)
// 2. Device API Keys - for IoT devices sending sensor data
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is required. Set via environment variable Jwt__Secret.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SensorHub";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SensorHub";

builder.Services.AddAuthentication(options =>
    {
        // JWT is the default scheme
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Configure how JWT tokens are validated
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Check the token was issued by a trusted issuer
            ValidateAudience = true,          // Check the token is meant for trusted audience
            ValidateLifetime = true,          // Check the token hasn't expired
            ValidateIssuerSigningKey = true,  // Verify the signature
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    })
    // Register our custom device API key authentication handler
    .AddScheme<AuthenticationSchemeOptions, DeviceApiKeyHandler>(
        DeviceApiKeyHandler.SchemeName, null);

builder.Services.AddAuthorization();

// Scoped = one instance per HTTP request
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AlertService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<ReadingService>();

// Controllers and Swagger/OpenAPI documentation
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SensorHub API", Version = "v1" });

    // Add JWT Bearer authentication to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add Device API Key authentication to Swagger UI
    c.AddSecurityDefinition("DeviceApiKey", new OpenApiSecurityScheme
    {
        Description = "Device API Key",
        Name = "X-Device-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // Make Swagger UI show the "Authorize" button for JWT
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SensorHubDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Apply migrations
    db.Database.Migrate();

    // Seed roles if they don't exist
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    if (!await roleManager.RoleExistsAsync("User"))
        await roleManager.CreateAsync(new IdentityRole("User"));

    // Seed admin user only in Development when SEED_ADMIN=true
    var seedAdmin = builder.Configuration["SEED_ADMIN"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    if (app.Environment.IsDevelopment() && seedAdmin)
    {
        var adminEmail = builder.Configuration["SEED_ADMIN_EMAIL"];
        var adminPassword = builder.Configuration["SEED_ADMIN_PASSWORD"];

        if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
        {
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
                await userManager.CreateAsync(admin, adminPassword);
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}


app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health endpoint for container orchestration
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();


public partial class Program { }
