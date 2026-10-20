using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniCar.API.Data;
using UniCar.API.DTOs;
using UniCar.API.Models;
using UniCar.API.Services;

namespace UniCar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int ResetCodeMinutes = 15;
    private const int ResetMaxAttempts = 5;

    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IEmailSender _email;
    private readonly IWebHostEnvironment _env;

    public AuthController(AppDbContext context, IConfiguration config, IEmailSender email, IWebHostEnvironment env)
    {
        _context = context;
        _config = config;
        _email = email;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();

        if (!normalizedEmail.EndsWith("@positivo.edu.br") && !normalizedEmail.EndsWith("up.edu.br"))
        {
            return BadRequest(new { message = "O e-mail deve pertencer aos domínios institucionais (@positivo.edu.br ou @up.edu.br)." });
        }

        var userType = (dto.UserType ?? "BOTH").Trim().ToUpperInvariant();
        if (userType is not ("PASSENGER" or "DRIVER" or "BOTH"))
        {
            return BadRequest(new { message = "Tipo de usuário inválido." });
        }

        // Motorista (ou "os dois") precisa ter CNH válida.
        string? cnhNumber = null;
        string? cnhCategory = null;
        DateOnly? cnhExpiry = null;

        if (userType is "DRIVER" or "BOTH")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3)); // horário de Brasília
            var cnhError = CnhValidator.Validate(dto.CnhNumber, dto.CnhCategory, dto.CnhExpiry, today);
            if (cnhError is not null)
            {
                return BadRequest(new { message = cnhError });
            }

            cnhNumber = CnhValidator.OnlyDigits(dto.CnhNumber);
            cnhCategory = CnhValidator.NormalizeCategory(dto.CnhCategory);
            cnhExpiry = dto.CnhExpiry;
        }

        var ra = dto.RaEnrollment.Trim();

        var exists = await _context.Users.AnyAsync(u =>
            u.Email == normalizedEmail ||
            u.RaEnrollment == ra ||
            (cnhNumber != null && u.CnhNumber == cnhNumber));
        if (exists)
        {
            return Conflict(new { message = "E-mail, RA/Matrícula ou CNH já se encontram registados." });
        }

        var user = new User
        {
            Name = dto.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Phone = dto.Phone.Trim(),
            RaEnrollment = ra,
            UserType = userType,
            CnhNumber = cnhNumber,
            CnhCategory = cnhCategory,
            CnhExpiry = cnhExpiry
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        return Ok(BuildAuthResponse(user));
    }

    /// <summary>
    /// Gera um código de 6 dígitos (válido por 15 minutos) e envia por e-mail.
    /// A resposta é sempre a mesma, exista ou não a conta, para não revelar quais e-mails estão cadastrados.
    /// Em Development o código também volta no campo devCode, para testar sem servidor de e-mail.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        var normalizedEmail = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        string? devCode = null;

        if (user != null)
        {
            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            user.PasswordResetCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
            user.PasswordResetExpiresAt = DateTime.UtcNow.AddMinutes(ResetCodeMinutes);
            user.PasswordResetAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _email.SendPasswordResetCodeAsync(user.Email, user.Name, code);

            if (_env.IsDevelopment()) devCode = code;
        }

        return Ok(new
        {
            message = "Se o e-mail estiver cadastrado, enviamos um código de 6 dígitos.",
            devCode
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        const string invalid = "Código inválido ou expirado. Peça um novo código.";

        var normalizedEmail = dto.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null
            || user.PasswordResetCodeHash == null
            || user.PasswordResetExpiresAt == null
            || user.PasswordResetExpiresAt < DateTime.UtcNow
            || user.PasswordResetAttempts >= ResetMaxAttempts)
        {
            return BadRequest(new { message = invalid });
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Code.Trim(), user.PasswordResetCodeHash))
        {
            user.PasswordResetAttempts++;
            await _context.SaveChangesAsync();
            return BadRequest(new { message = invalid });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetExpiresAt = null;
        user.PasswordResetAttempts = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Senha alterada com sucesso." });
    }

    private AuthResponseDto BuildAuthResponse(User user) => new()
    {
        Token = GenerateJwtToken(user),
        UserId = user.Id,
        Name = user.Name,
        Email = user.Email
    };

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
