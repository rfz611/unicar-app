using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniCar.API.Data;
using UniCar.API.DTOs;
using UniCar.API.Models;

namespace UniCar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();

    if (!normalizedEmail.EndsWith("@positivo.edu.br") && !normalizedEmail.EndsWith("up.edu.br"))        {
            return BadRequest(new { message = "O e-mail deve pertencer aos domÃ­nios institucionais (@positivo.edu.br ou @up.edu.br)." });
        }

        var exists = await _context.Users.AnyAsync(u => u.Email == normalizedEmail || u.RaEnrollment == dto.RaEnrollment);
        if (exists)
        {
            return Conflict(new { message = "E-mail ou RA/MatrÃ­cula jÃ¡ se encontram registados." });
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone.Trim(),
            RaEnrollment = dto.RaEnrollment.Trim(),
            UserType = dto.UserType
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciais invÃ¡lidas." });
        }

        var token = GenerateJwtToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email
        });
    }

    private string GenerateJwtToken(User user)
    {
        var keyString = _config["Jwt:Key"] ?? "chave_secreta_super_segura_unicar_api_2026_jwt_token";
        var issuer = _config["Jwt:Issuer"] ?? "UniCarAPI";
        var audience = _config["Jwt:Audience"] ?? "UniCarApp";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", user.Name),
            new Claim("role", user.UserType)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
