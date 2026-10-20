using Microsoft.EntityFrameworkCore;
using UniCar.Api.Domain;

namespace UniCar.Api.Data;

/// <summary>
/// Faculdades e campi iniciais de Curitiba.
/// Confira com alunos de cada uma os domínios de e-mail e ajuste as coordenadas dos campi (são aproximadas).
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Universities.AnyAsync(ct)) return;

        db.Universities.AddRange(
            Uni("Universidade Federal do Paraná", "UFPR", new[] { "ufpr.br" },
                ("Centro Politécnico", "Av. Cel. Francisco H. dos Santos, 100 - Jardim das Américas", -25.4507, -49.2316),
                ("Prédio Histórico", "Praça Santos Andrade, 50 - Centro", -25.4297, -49.2611)),
            Uni("Pontifícia Universidade Católica do Paraná", "PUCPR", new[] { "pucpr.edu.br", "pucpr.br" },
                ("Campus Curitiba", "R. Imaculada Conceição, 1155 - Prado Velho", -25.4517, -49.2513)),
            Uni("Universidade Tecnológica Federal do Paraná", "UTFPR", new[] { "utfpr.edu.br" },
                ("Câmpus Curitiba, Sede Centro", "Av. Sete de Setembro, 3165 - Rebouças", -25.4393, -49.2688)),
            Uni("Universidade Positivo", "Positivo", new[] { "up.edu.br" },
                ("Campus Ecoville", "R. Prof. Pedro Viriato Parigot de Souza, 5300 - Campo Comprido", -25.4446, -49.3561)),
            Uni("FAE Centro Universitário", "FAE", new[] { "fae.edu", "fae.edu.br" },
                ("Campus Centro", "R. 24 de Maio, 135 - Centro", -25.4361, -49.2701))
        );
        await db.SaveChangesAsync(ct);
    }

    private static University Uni(string name, string shortName, string[] domains,
        params (string Name, string Address, double Lat, double Lng)[] campuses) => new()
    {
        Name = name,
        ShortName = shortName,
        Domains = domains.Select(d => new UniversityDomain { Domain = d }).ToList(),
        Campuses = campuses.Select(c => new Campus { Name = c.Name, Address = c.Address, Latitude = c.Lat, Longitude = c.Lng }).ToList()
    };
}
