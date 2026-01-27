using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SensorHub.Api.Models;
using SensorHub.Api.Services;

namespace SensorHub.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    JwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtService.GenerateToken(user, roles);
        var expiration = DateTime.UtcNow.AddHours(1);

        return Ok(new LoginResponse(token, expiration));
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { message = "User already exists" });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var role = request.Role == "Admin" ? "Admin" : "User";
        await userManager.AddToRoleAsync(user, role);

        return Ok(new { message = "User registered successfully" });
    }
}
