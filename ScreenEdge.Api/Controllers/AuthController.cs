using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ScreenEdge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var validUsername = _configuration["Auth:Username"] ?? "admin";
        var validPassword = _configuration["Auth:Password"] ?? "ScreenEdge@2025";

        if (request.Username != validUsername || request.Password != validPassword)
            return Unauthorized(new { message = "Invalid credentials" });

        var token = GenerateJwtToken(request.Username);
        return Ok(new { token, username = request.Username });
    }

    private string GenerateJwtToken(string username)
    {
        var secret = _configuration["Jwt:Secret"] ?? "ScreenEdge-Super-Secret-Key-2025-Do-Not-Share!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "ScreenEdge",
            audience: "ScreenEdge",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
