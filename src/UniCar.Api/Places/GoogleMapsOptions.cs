namespace UniCar.Api.Places;

public class GoogleMapsOptions
{
    public const string Section = "GoogleMaps";

    /// <summary>Chave do Google Cloud com "Places API (New)", "Routes API" e "Geocoding API" ativadas. Fica só no servidor.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Sem ApiKey, calcular preço/rota por estimativa em vez de responder 503. Ligado só em Development.</summary>
    public bool EstimateWhenNotConfigured { get; set; }

    /// <summary>Região metropolitana de Curitiba. Endereços fora desse retângulo não aparecem na busca.</summary>
    public double MinLatitude { get; set; } = -25.80;
    public double MinLongitude { get; set; } = -49.60;
    public double MaxLatitude { get; set; } = -25.20;
    public double MaxLongitude { get; set; } = -48.95;
}
