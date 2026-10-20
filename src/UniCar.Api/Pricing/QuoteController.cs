using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCar.Api.Common;
using UniCar.Api.Places;

namespace UniCar.Api.Pricing;

public record QuoteRequest(double OriginLat, double OriginLng, double DestinationLat, double DestinationLng);

public record QuoteResponse(double Km, int Minutes, string? EncodedPolyline, PriceQuote Now, PriceQuote Scheduled);

[ApiController]
[Route("api/quote")]
[Authorize]
public class QuoteController(IMapsClient maps, PricingService pricing) : ControllerBase
{
    /// <summary>Calcula a rota real pelas ruas e devolve o preço de "Pedir agora" e de "Carona marcada".</summary>
    [HttpPost]
    public async Task<IActionResult> Quote(QuoteRequest req, CancellationToken ct)
    {
        var route = await maps.ComputeRouteAsync(
            new LatLng(req.OriginLat, req.OriginLng), new LatLng(req.DestinationLat, req.DestinationLng), ct);
        if (route is null) return BadRequest(new ApiError("no_route", "Não encontramos rota entre esses pontos."));

        return Ok(new QuoteResponse(
            Math.Round(route.Km, 1), (int)Math.Round(route.Minutes), route.EncodedPolyline,
            pricing.Quote(route.Km, route.Minutes, RideMode.Now),
            pricing.Quote(route.Km, route.Minutes, RideMode.Scheduled)));
    }
}
