using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Auth;

public enum CodeRequestResult { Sent, TooSoon, TooMany }
public enum CodeCheckResult { Valid, Invalid, Expired, TooManyAttempts }

public class EmailCodeService(AppDbContext db, IEmailSender email, IOptions<EmailCodeOptions> opt, TimeProvider clock)
{
    private readonly EmailCodeOptions _o = opt.Value;

    public async Task<CodeRequestResult> SendAsync(string address, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var recent = await db.EmailVerificationCodes
            .Where(c => c.Email == address && c.CreatedAt > now.AddHours(-1))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        if (recent.Count > 0 && recent[0].CreatedAt > now.AddSeconds(-_o.ResendCooldownSeconds)) return CodeRequestResult.TooSoon;
        if (recent.Count >= _o.MaxCodesPerHour) return CodeRequestResult.TooMany;

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        db.EmailVerificationCodes.Add(new EmailVerificationCode
        {
            Email = address,
            CodeHash = Hash(address, code),
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(_o.ExpiresMinutes)
        });
        await db.SaveChangesAsync(ct);

        await email.SendAsync(address, $"{code} é o seu código do UniCar",
            $"Seu código de verificação do UniCar é {code}.\n\nEle vale por {_o.ExpiresMinutes} minutos. " +
            "Se não foi você que pediu, ignore este e-mail.", ct);
        return CodeRequestResult.Sent;
    }

    public async Task<CodeCheckResult> CheckAsync(string address, string code, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var last = await db.EmailVerificationCodes
            .Where(c => c.Email == address && c.UsedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (last is null) return CodeCheckResult.Invalid;
        if (last.ExpiresAt < now) return CodeCheckResult.Expired;
        if (last.Attempts >= _o.MaxAttempts) return CodeCheckResult.TooManyAttempts;

        var ok = CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(last.CodeHash), Encoding.ASCII.GetBytes(Hash(address, code.Trim())));

        if (!ok)
        {
            last.Attempts++;
            await db.SaveChangesAsync(ct);
            return last.Attempts >= _o.MaxAttempts ? CodeCheckResult.TooManyAttempts : CodeCheckResult.Invalid;
        }

        last.UsedAt = now;
        await db.SaveChangesAsync(ct);
        return CodeCheckResult.Valid;
    }

    private static string Hash(string address, string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(address + ":" + code)));
}
