using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCar.Api.Common;

namespace UniCar.Api.Places;

[ApiController]
[Route("api/places")]
[Authorize]
public class PlacesController(IMapsClient maps) : ControllerBase
{
    /// <summary>
    /// Busca enquanto a pessoa digita. O app gera um sessionToken (um GUID) quando a busca começa
    /// e usa o mesmo até escolher um lugar: assim o Google cobra a busca inteira como uma só.
    /// </summary>
    [HttpGet("autocomplete")]
    public async Task<IActionResult> Autocomplete([FromQuery] string q, [FromQuery] string session,
        [FromQuery] double? lat, [FromQuery] double? lng, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Ok(Array.Empty<PlaceSuggestion>());
        if (string.IsNullOrWhiteSpace(session)) return BadRequest(new ApiError("missing_session", "Envie o parâmetro session."));
        var near = lat is not null && lng is not null ? new LatLng(lat.Value, lng.Value) : null;
        return Ok(await maps.AutocompleteAsync(q.Trim(), session, near, ct));
    }

    /// <summary>Quando a pessoa toca numa sugestão: devolve coordenadas e bairro. Encerra a sessão de busca.</summary>
    [HttpGet("{placeId}")]
    public async Task<IActionResult> Details(string placeId, [FromQuery] string? session, CancellationToken ct)
    {
        var place = await maps.GetPlaceAsync(placeId, session, ct);
        return place is null ? NotFound(new ApiError("place_not_found", "Lugar não encontrado.")) : Ok(place);
    }

    /// <summary>"Localização atual": transforma o GPS do celular em endereço.</summary>
    [HttpGet("reverse")]
    public async Task<IActionResult> Reverse([FromQuery] double lat, [FromQuery] double lng, CancellationToken ct)
    {
        var place = await maps.ReverseGeocodeAsync(new LatLng(lat, lng), ct);
        return place is null ? NotFound(new ApiError("address_not_found", "Não achamos um endereço para essa posição.")) : Ok(place);
    }
}
