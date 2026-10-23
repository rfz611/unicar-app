using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Users;

namespace UniCar.Api.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext db,
    UniversityResolver universities,
    EmailCodeService codes,
    TokenService tokens,
    SensitiveData sensitive,
    IOptions<EmailCodeOptions> codeOptions,
    TimeProvider clock) : ControllerBase
{
    /// <summary>Passo 1: manda o código para o e-mail da faculdade.</summary>
    [HttpPost("request-code")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestCode(RequestCodeRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!new EmailAddressAttribute().IsValid(email))
            return BadRequest(new ApiError("invalid_email", "Digite um e-mail válido."));

        var uni = await universities.FindByEmailAsync(email, ct);
        if (uni is null)
            return BadRequest(new ApiError("not_university_email",
                "Esse e-mail não é de uma faculdade parceira. Use o institucional, por exemplo nome@ufpr.br."));

        return await codes.SendAsync(email, ct) switch
        {
            CodeRequestResult.TooSoon => StatusCode(429, new ApiError("too_soon", "Espere um minuto antes de pedir outro código.")),
            CodeRequestResult.TooMany => StatusCode(429, new ApiError("too_many", "Muitos códigos pedidos. Tente de novo em uma hora.")),
            _ => Ok(new RequestCodeResponse(uni.ShortName, codeOptions.Value.ExpiresMinutes))
        };
    }

    /// <summary>Passo 2: confere o código. Devolve login direto ou o token para terminar o cadastro.</summary>
    [HttpPost("verify-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCode(VerifyCodeRequest req, CancellationToken ct)
    {
        var email = (req.Email ?? string.Empty).Trim().ToLowerInvariant();
        var result = await codes.CheckAsync(email, req.Code ?? string.Empty, ct);
        if (result != CodeCheckResult.Valid)
        {
            return BadRequest(result switch
            {
                CodeCheckResult.Expired => new ApiError("code_expired", "O código expirou. Peça um novo."),
                CodeCheckResult.TooManyAttempts => new ApiError("code_locked", "Muitas tentativas erradas. Peça um novo código."),
                _ => new ApiError("code_invalid", "Código incorreto. Confira o e-mail.")
            });
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        return Ok(user is null
            ? new VerifyCodeResponse(true, null, tokens.CreateSignupToken(email))
            : new VerifyCodeResponse(false, tokens.CreateAccessToken(user), null));
    }

    /// <summary>Passo 3 (só na primeira vez): dados pessoais. Exige o token de cadastro do passo 2.</summary>
    [HttpPost("register")]
    [Authorize(Policy = TokenPurpose.Signup)]
    public async Task<IActionResult> Register(RegisterRequest req, CancellationToken ct)
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        if (email is null) return Unauthorized();

        var errors = UserValidation.Validate(req.FullName, req.BirthDate, req.Cpf, req.Phone, req.Course, req.Period, req.Shift,
            LocalTime.Today(clock), out var birth, out var shift);
        if (errors is not null) return BadRequest(errors);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            return Conflict(new ApiError("already_registered", "Esse e-mail já tem conta. Entre de novo."));

        var cpfDigits = Cpf.Digits(req.Cpf);
        var cpfHash = sensitive.Hash(cpfDigits);
        if (await db.Users.AnyAsync(u => u.CpfHash == cpfHash, ct))
            return Conflict(new ApiError("cpf_in_use", "Já existe uma conta com esse CPF."));

        var uni = await universities.FindByEmailAsync(email, ct);
        if (uni is null) return BadRequest(new ApiError("not_university_email", "Faculdade não encontrada para esse e-mail."));

        var user = new User
        {
            Email = email,
            UniversityId = uni.Id,
            FullName = UserValidation.CleanName(req.FullName),
            BirthDate = birth,
            Phone = Phone.Format(req.Phone),
            CpfEncrypted = sensitive.Encrypt(cpfDigits),
            CpfHash = cpfHash,
            Course = req.Course,
            Period = req.Period,
            Shift = shift,
            EmailVerifiedAt = clock.GetUtcNow(),
            Preferences = new UserPreferences()
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        user.University = uni;
        return Ok(new AuthResponse(tokens.CreateAccessToken(user), UserDto.From(user, cpfDigits)));
    }
}
