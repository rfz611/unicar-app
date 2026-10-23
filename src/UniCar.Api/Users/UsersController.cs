using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Users;

[ApiController]
[Route("api")]
[Authorize]
public class UsersController(AppDbContext db, SensitiveData sensitive, TimeProvider clock) : ControllerBase
{
    private static readonly string[] Genders = { "Mulher", "Homem", "Prefiro não dizer" };

    /// <summary>O usuário logado com tudo que o <see cref="UserDto"/> precisa.</summary>
    private Task<User?> LoadMeAsync(CancellationToken ct) =>
        db.Users
            .Include(u => u.University).ThenInclude(x => x!.Campuses)
            .Include(u => u.Campus)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Id == User.UserId(), ct);

    [HttpGet("users/me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await LoadMeAsync(ct);
        if (user is null) return NotFound();
        return Ok(UserDto.From(user, sensitive.Decrypt(user.CpfEncrypted)));
    }

    /// <summary>Editar perfil. CPF e e-mail não mudam por aqui.</summary>
    [HttpPut("users/me")]
    public async Task<IActionResult> UpdateMe(UpdateMeRequest req, CancellationToken ct)
    {
        var user = await LoadMeAsync(ct);
        if (user is null) return NotFound();
        var cpf = sensitive.Decrypt(user.CpfEncrypted);

        var error = UserValidation.Validate(req.FullName, req.BirthDate, cpf, req.Phone, req.Course, req.Period, req.Shift,
            LocalTime.Today(clock), out var birth, out var shift);
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
        if (req.ShowCourse is { } showCourse)
        {
            user.Preferences ??= new UserPreferences { UserId = user.Id };
            user.Preferences.ShowCourse = showCourse;
        }
        await db.SaveChangesAsync(ct);

        return Ok(UserDto.From(user, cpf));
    }

    /// <summary>Escolhe o campus do atalho "Minha faculdade" (só campi da própria faculdade).</summary>
    [HttpPut("users/me/campus")]
    public async Task<IActionResult> ChooseCampus(ChooseCampusRequest req, CancellationToken ct)
    {
        var user = await LoadMeAsync(ct);
        if (user is null) return NotFound();
        var campus = user.University!.Campuses.FirstOrDefault(c => c.Id == req.CampusId);
        if (campus is null) return BadRequest(new ApiError("invalid_campus", "Escolha um campus da sua faculdade."));

        user.CampusId = campus.Id;
        user.Campus = campus;
        await db.SaveChangesAsync(ct);
        return Ok(UserDto.From(user, sensitive.Decrypt(user.CpfEncrypted)));
    }

    [HttpGet("users/me/preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken ct) =>
        Ok(PreferencesDto.From(await PreferencesAsync(ct)));

    [HttpPut("users/me/preferences")]
    public async Task<IActionResult> UpdatePreferences(PreferencesDto req, CancellationToken ct)
    {
        if (!Enum.TryParse<Theme>(req.Theme, ignoreCase: true, out var theme) || !Enum.IsDefined(theme))
            return BadRequest(new ApiError("invalid_theme", "Tema deve ser System, Light ou Dark."));

        var prefs = await PreferencesAsync(ct);
        prefs.Theme = theme;
        prefs.RideAlerts = req.RideAlerts;
        prefs.AlwaysShareTrip = req.AlwaysShareTrip;
        prefs.ShowCourse = req.ShowCourse;
        await db.SaveChangesAsync(ct);
        return Ok(PreferencesDto.From(prefs));
    }

    /// <summary>Preferências do usuário logado; cria com os padrões se ainda não existirem.</summary>
    private async Task<UserPreferences> PreferencesAsync(CancellationToken ct)
    {
        var id = User.UserId()!.Value;
        var prefs = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == id, ct);
        if (prefs is null)
        {
            prefs = new UserPreferences { UserId = id };
            db.UserPreferences.Add(prefs);
            await db.SaveChangesAsync(ct);
        }
        return prefs;
    }

    [HttpGet("courses")]
    [AllowAnonymous]
    public IActionResult ListCourses() => Ok(Courses.All);

    [HttpGet("universities")]
    [AllowAnonymous]
    public async Task<IActionResult> ListUniversities(CancellationToken ct) =>
        Ok(await db.Universities
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id, u.Name, u.ShortName,
                Domains = u.Domains.Select(d => d.Domain),
                Campuses = u.Campuses.OrderBy(c => c.Id).Select(c => new { c.Id, c.Name, c.Address, c.Latitude, c.Longitude })
            })
            .ToListAsync(ct));
}
