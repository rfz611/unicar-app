using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Auth;
using UniCar.Api.Common;
using UniCar.Api.Users;

namespace UniCar.Api.Data;

public record DevLoginRequest(string Email);

/// <summary>404 fora de Development ou com Demo:Enabled = false. Roda antes do binding e da ação.</summary>
public class DevOnlyAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        var services = context.HttpContext.RequestServices;
        var env = services.GetRequiredService<IWebHostEnvironment>();
        var demo = services.GetRequiredService<IOptions<DemoOptions>>().Value;
        if (!env.IsDevelopment() || !demo.Enabled) context.Result = new NotFoundResult();
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}

/// <summary>
/// Atalhos para a apresentação. Só existem em Development com Demo:Enabled = true;
/// em qualquer outro caso respondem 404, como se não existissem.
/// </summary>
[ApiController]
[Route("api/dev")]
[AllowAnonymous]
[DevOnly]
public class DevController(AppDbContext db, DemoSeed demo, TokenService tokens, SensitiveData sensitive) : ControllerBase
{
    /// <summary>Contas da demonstração.</summary>
    [HttpGet("demo")]
    public IActionResult Accounts() => Ok(DemoSeed.Accounts);

    /// <summary>Apaga e recria a demonstração com horários a partir de agora. Rode antes de apresentar.</summary>
    [HttpPost("demo/reset")]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        await demo.ResetAsync(ct);
        return Ok(DemoSeed.Accounts);
    }

    /// <summary>Entra numa conta *.demo@ sem código por e-mail.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(DevLoginRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!email.Contains(DemoSeed.Marker))
            return BadRequest(new ApiError("not_demo_account", "Só contas de demonstração (*.demo@...)."));

        var user = await db.Users
            .Include(u => u.University).ThenInclude(x => x!.Campuses)
            .Include(u => u.Campus)
            .Include(u => u.Preferences)
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return NotFound(new ApiError("not_found", "Conta de demonstração não encontrada. Rode POST /api/dev/demo/reset."));

        return Ok(new AuthResponse(tokens.CreateAccessToken(user), UserDto.From(user, sensitive.Decrypt(user.CpfEncrypted))));
    }
}
