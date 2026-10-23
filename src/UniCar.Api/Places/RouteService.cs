using Microsoft.Extensions.Options;

namespace UniCar.Api.Places;

/// <summary>
/// Rota para preço e tempo. Usa o Google; sem chave configurada:
/// - GoogleMaps:EstimateWhenNotConfigured = true (Development): estimativa em linha reta (Estimated = true);
/// - false (padrão, produção): lança MapsNotConfiguredException e a API responde 503.
/// </summary>
public class RouteService(IMapsClient maps, IOptions<GoogleMapsOptions> opt)
{
    public async Task<(RouteInfo? Route, bool Estimated)> GetAsync(LatLng origin, LatLng destination, CancellationToken ct = default)
    {
        try
        {
            return (await maps.ComputeRouteAsync(origin, destination, ct), false);
        }
        catch (MapsNotConfiguredException) when (opt.Value.EstimateWhenNotConfigured)
        {
            return (Geo.EstimateRoute(origin, destination), true);
        }
    }

    /// <summary>Para valores só indicativos ("passa por você", ETA): nunca falha por falta de chave.</summary>
    public async Task<(RouteInfo Route, bool Estimated)> GetOrEstimateAsync(LatLng origin, LatLng destination, CancellationToken ct = default)
    {
        try
        {
            var route = await maps.ComputeRouteAsync(origin, destination, ct);
            if (route is not null) return (route, false);
        }
        catch (MapsNotConfiguredException) { }
        return (Geo.EstimateRoute(origin, destination), true);
    }
}
