using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace UniCar.Api.Places;

/// <summary>
/// Lugares de Curitiba para desenvolver e apresentar sem chave do Google: campi (mesmas coordenadas do
/// seed), lugares do protótipo e bairros. Só é usado quando GoogleMaps:EstimateWhenNotConfigured está
/// ligado e não há ApiKey. Coordenadas aproximadas.
/// </summary>
public static class LocalPlaces
{
    public record Place(string Id, string Name, string Address, string Neighborhood, double Latitude, double Longitude, string Aliases = "");

    public static readonly IReadOnlyList<Place> All = new Place[]
    {
        new("local:ufpr-politecnico", "UFPR - Centro Politécnico", "Av. Cel. Francisco H. dos Santos, 100 - Jardim das Américas, Curitiba - PR", "Jardim das Américas", -25.4507, -49.2316, "ufpr federal politecnico"),
        new("local:ufpr-historico", "UFPR - Prédio Histórico (Praça Santos Andrade)", "Praça Santos Andrade, 50 - Centro, Curitiba - PR", "Centro", -25.4297, -49.2611, "ufpr federal reitoria santos andrade historico"),
        new("local:pucpr", "PUCPR - Pontifícia Universidade Católica do Paraná", "R. Imaculada Conceição, 1155 - Prado Velho, Curitiba - PR", "Prado Velho", -25.4517, -49.2513, "puc pucpr catolica pontificia"),
        new("local:utfpr", "UTFPR - Câmpus Curitiba, Sede Centro", "Av. Sete de Setembro, 3165 - Rebouças, Curitiba - PR", "Rebouças", -25.4393, -49.2688, "utfpr tecnologica federal cefet"),
        new("local:positivo", "Universidade Positivo - Campus Ecoville", "R. Prof. Pedro Viriato Parigot de Souza, 5300 - Campo Comprido, Curitiba - PR", "Campo Comprido", -25.4446, -49.3561, "up positivo universidade ecoville"),
        new("local:fae", "FAE Centro Universitário", "R. 24 de Maio, 135 - Centro, Curitiba - PR", "Centro", -25.4361, -49.2701, "fae"),
        new("local:patio-batel", "Shopping Pátio Batel", "Av. do Batel, 1868 - Batel, Curitiba - PR", "Batel", -25.4436, -49.2915, "shopping patio batel"),
        new("local:estacao", "Shopping Estação", "Av. Sete de Setembro, 2775 - Rebouças, Curitiba - PR", "Rebouças", -25.4397, -49.2780, "shopping estacao"),
        new("local:shopping-curitiba", "Shopping Curitiba", "R. Brig. Franco, 2300 - Centro, Curitiba - PR", "Centro", -25.4357, -49.2797, "shopping curitiba"),
        new("local:rodoferroviaria", "Rodoferroviária de Curitiba", "Av. Pres. Affonso Camargo, 330 - Jardim Botânico, Curitiba - PR", "Jardim Botânico", -25.4372, -49.2556, "rodoviaria rodoferroviaria onibus"),
        new("local:jardim-botanico", "Jardim Botânico de Curitiba", "R. Eng. Ostoja Roguski - Jardim Botânico, Curitiba - PR", "Jardim Botânico", -25.4419, -49.2390, "jardim botanico estufa"),
        new("local:barigui", "Parque Barigui", "Av. Cândido Hartmann - Bigorrilho, Curitiba - PR", "Bigorrilho", -25.4224, -49.3076, "parque barigui"),
        new("local:hc", "Hospital de Clínicas da UFPR", "R. Gen. Carneiro, 181 - Alto da Glória, Curitiba - PR", "Alto da Glória", -25.4245, -49.2614, "hospital clinicas hc"),
        new("local:batel", "Batel", "Batel, Curitiba - PR", "Batel", -25.4420, -49.2890),
        new("local:agua-verde", "Água Verde", "Água Verde, Curitiba - PR", "Água Verde", -25.4560, -49.2810),
        new("local:portao", "Portão", "Portão, Curitiba - PR", "Portão", -25.4740, -49.2930),
        new("local:centro", "Centro", "Centro, Curitiba - PR", "Centro", -25.4296, -49.2713),
        new("local:centro-civico", "Centro Cívico", "Centro Cívico, Curitiba - PR", "Centro Cívico", -25.4170, -49.2680),
        new("local:cabral", "Cabral", "Cabral, Curitiba - PR", "Cabral", -25.4100, -49.2550),
        new("local:bacacheri", "Bacacheri", "Bacacheri, Curitiba - PR", "Bacacheri", -25.3990, -49.2320),
        new("local:reboucas", "Rebouças", "Rebouças, Curitiba - PR", "Rebouças", -25.4450, -49.2650),
        new("local:jardim-americas", "Jardim das Américas", "Jardim das Américas, Curitiba - PR", "Jardim das Américas", -25.4560, -49.2290),
        new("local:prado-velho", "Prado Velho", "Prado Velho, Curitiba - PR", "Prado Velho", -25.4530, -49.2520),
        new("local:bigorrilho", "Bigorrilho", "Bigorrilho, Curitiba - PR", "Bigorrilho", -25.4290, -49.2980),
        new("local:santa-felicidade", "Santa Felicidade", "Santa Felicidade, Curitiba - PR", "Santa Felicidade", -25.4030, -49.3300),
    };

    /// <summary>Busca tolerante (sem acento, por pedaço de palavra), como a do protótipo. Campi primeiro.</summary>
    public static IReadOnlyList<Place> Search(string query, int take = 6)
    {
        var terms = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0) return Array.Empty<Place>();
        return All
            .Select(p => (Place: p, Words: Normalize($"{p.Name} {p.Address} {p.Aliases}").Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            .Select(x => (x.Place, Score: terms.Sum(t => x.Words.Max(w => w == t ? 4 : w.StartsWith(t) ? 3 : t.Length >= 3 && w.Contains(t) ? 2 : 0)),
                Ok: terms.All(t => x.Words.Any(w => w.StartsWith(t) || (t.Length >= 3 && w.Contains(t))))))
            .Where(x => x.Ok)
            .OrderByDescending(x => x.Score + (x.Place.Aliases.Length > 0 ? 0.5 : 0))
            .Take(take)
            .Select(x => x.Place)
            .ToList();
    }

    public static Place? Find(string id) => All.FirstOrDefault(p => p.Id == id);

    public static Place Nearest(double lat, double lng) => All.MinBy(p => Geo.Km(lat, lng, p.Latitude, p.Longitude))!;

    public static string Normalize(string text)
    {
        var d = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static PlaceDetails ToDetails(Place p) => new(p.Id, p.Name, p.Address, p.Neighborhood, p.Latitude, p.Longitude);
}

/// <summary>
/// IMapsClient que usa o Google quando há chave; sem chave e com EstimateWhenNotConfigured (Development),
/// responde busca, detalhes e endereço do GPS com <see cref="LocalPlaces"/>. A rota continua com o
/// <see cref="RouteService"/>, que estima.
/// </summary>
public class FallbackMapsClient(GoogleMapsClient google, IOptions<GoogleMapsOptions> opt) : IMapsClient
{
    private bool UseLocal => string.IsNullOrWhiteSpace(opt.Value.ApiKey) && opt.Value.EstimateWhenNotConfigured;

    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string input, string sessionToken, LatLng? near, CancellationToken ct = default) =>
        UseLocal
            ? Task.FromResult<IReadOnlyList<PlaceSuggestion>>(LocalPlaces.Search(input).Select(p => new PlaceSuggestion(p.Id, p.Name, p.Address)).ToList())
            : google.AutocompleteAsync(input, sessionToken, near, ct);

    public Task<PlaceDetails?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct = default)
    {
        if (placeId.StartsWith("local:"))
        {
            var p = LocalPlaces.Find(placeId);
            return Task.FromResult(p is null ? null : LocalPlaces.ToDetails(p));
        }
        return google.GetPlaceAsync(placeId, sessionToken, ct);
    }

    /// <summary>Sem Google: o ponto do GPS com o bairro conhecido mais próximo.</summary>
    public Task<PlaceDetails?> ReverseGeocodeAsync(LatLng point, CancellationToken ct = default)
    {
        if (!UseLocal) return google.ReverseGeocodeAsync(point, ct);
        var near = LocalPlaces.Nearest(point.Latitude, point.Longitude);
        var neighborhood = Geo.Km(point.Latitude, point.Longitude, near.Latitude, near.Longitude) <= 3 ? near.Neighborhood : null;
        var label = neighborhood is null ? "Localização atual" : $"Perto de {near.Name}";
        return Task.FromResult<PlaceDetails?>(new PlaceDetails("gps", label,
            neighborhood is null ? "Curitiba - PR" : $"{neighborhood}, Curitiba - PR", neighborhood, point.Latitude, point.Longitude));
    }

    public Task<RouteInfo?> ComputeRouteAsync(LatLng origin, LatLng destination, CancellationToken ct = default) =>
        google.ComputeRouteAsync(origin, destination, ct);
}
