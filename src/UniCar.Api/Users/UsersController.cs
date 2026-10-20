using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;

namespace UniCar.Api.Users;

[ApiController]
[Route("api")]
[Authorize]
public class UsersController(AppDbContext db, SensitiveData sensitive, TimeProvider clock) : ControllerBase
{
    private static readonly string[] Genders = { "Mulher", "Homem", "Prefiro não dizer" };

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;

    [HttpGet("users/me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.University).FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);
        if (user is null) return NotFound();
        return Ok(UserDto.From(user, sensitive.Decrypt(user.CpfEncrypted)));
    }

    /// <summary>Editar perfil. CPF e e-mail não mudam por aqui.</summary>
    [HttpPut("users/me")]
    public async Task<IActionResult> UpdateMe(UpdateMeRequest req, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.University).FirstOrDefaultAsync(u => u.Id == CurrentUserId, ct);
        if (user is null) return NotFound();
        var cpf = sensitive.Decrypt(user.CpfEncrypted);

        var error = UserValidation.Validate(req.FullName, req.BirthDate, cpf, req.Phone, req.Course, req.Period, req.Shift,
            DateOnly.FromDateTime(clock.GetLocalNow().DateTime), out var birth, out var shift);
        if (error is not null) return BadRequest(error);
        if (req.Gender is not null && !Genders.Contains(req.Gender))
            return BadRequest(new ApiError("invalid_gender", "Opção de gênero inválida."));
        if (req.Bio is { Length: > 140 }) return BadRequest(new ApiError("bio_too_long", "A bio tem no máximo 140 caracteres."));
        if (!string.IsNullOrWhiteSpace(req.TrustedContactPhone) && !Phone.IsValidMobile(req.TrustedContactPhone))
            return BadRequest(new ApiError("invalid_trusted_phone", "Telefone do contato com DDD, 11 números."));

        user.FullName = UserValidation.CleanName(req.FullName);
        user.BirthDate = birth;
        user.Phone = Phone.Format(req.Phone);
        user.Course = req.Course;
        user.Period = req.Period;
        user.Shift = shift;
        user.Gender = req.Gender;
        user.Bio = string.IsNullOrWhiteSpace(req.Bio) ? null : req.Bio.Trim();
        user.TrustedContactPhone = string.IsNullOrWhiteSpace(req.TrustedContactPhone) ? null : Phone.Format(req.TrustedContactPhone);
        await db.SaveChangesAsync(ct);

        return Ok(UserDto.From(user, cpf));
    }

    [HttpGet("courses")]
    [AllowAnonymous]
    public IActionResult ListCourses() => Ok(Courses.All);

    [HttpGet("universities")]
    [AllowAnonymous]
    public async Task<IActionResult> ListUniversities(CancellationToken ct) =>
        Ok(await db.Universities
            .Select(u => new
            {
                u.Id, u.Name, u.ShortName,
                Domains = u.Domains.Select(d => d.Domain),
                Campuses = u.Campuses.Select(c => new { c.Id, c.Name, c.Address, c.Latitude, c.Longitude })
            })
            .ToListAsync(ct));
}
