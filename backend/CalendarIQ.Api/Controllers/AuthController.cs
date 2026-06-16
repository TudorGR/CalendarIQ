using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CalendarIQ.Api.Data;
using CalendarIQ.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CalendarIQ.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
                return BadRequest(new { message = "Email already exists" });

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };

            _db.Users.Add(user);

            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(user.Id, user.Email);

            return StatusCode(201, new
            {
                message = "User registered successfully",
                token,
                user = new { id = user.Id, email = user.Email, name = user.Name }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering user: {ex}");
            return StatusCode(500, new { message = "Failed to register user", error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var user =
                await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                return NotFound(new { message = "User not found" });

            bool valid =
                BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.Password
                );

            if (!valid)
                return Unauthorized(new { message = "Invalid credentials" });

            var token = GenerateJwtToken(user.Id, user.Email);

            return Ok(new
            {
                token,
                user = new { id = user.Id, email = user.Email, name = user.Name }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error logging in user: {ex}");
            return StatusCode(500, new { message = "Failed to login", error = ex.Message });
        }
    }

    private string GenerateJwtToken(int userId, string email)
    {
        var jwtSecret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret missing.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var claims = new[]
        {
            new Claim("userId",userId.ToString()),
            new Claim(ClaimTypes.Email,email)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }
}