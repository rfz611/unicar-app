using Microsoft.EntityFrameworkCore;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Auth;

public class UniversityResolver(AppDbContext db)
{
    /// <summary>Acha a faculdade pelo domínio do e-mail. Aceita subdomínios (alunos.utfpr.edu.br vale como utfpr.edu.br).</summary>
    public async Task<University?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var at = email.LastIndexOf('@');
        if (at < 1 || at == email.Length - 1) return null;
        var host = email[(at + 1)..].ToLowerInvariant();

        var candidates = new List<string>();
        var parts = host.Split('.');
        for (var i = 0; i < parts.Length - 1; i++) candidates.Add(string.Join('.', parts[i..]));

        var match = await db.UniversityDomains
            .Where(d => candidates.Contains(d.Domain))
            .OrderByDescending(d => d.Domain.Length)
            .FirstOrDefaultAsync(ct);
        if (match is null) return null;
        return await db.Universities.Include(u => u.Campuses).FirstAsync(u => u.Id == match.UniversityId, ct);
    }
}
