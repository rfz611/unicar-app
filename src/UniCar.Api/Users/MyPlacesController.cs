using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Places;

namespace UniCar.Api.Users;

public record SavedPlaceDto(int Id, string Kind, string Label, string Address, string? Neighborhood, string? GooglePlaceId,
    double Latitude, double Longitude)
{
    public static SavedPlaceDto From(SavedPlace p) =>
        new(p.Id, p.Kind.ToString(), p.Label, p.Address, p.Neighborhood, p.GooglePlaceId, p.Latitude, p.Longitude);
}

/// <summary>O app manda o que recebeu de GET /api/places/{id} (ou /reverse).</summary>
public record SavePlaceRequest(string Address, string? Neighborhood, string? GooglePlaceId, double Latitude, double Longitude,
    string? Label = null);

/// <summary>Lugares do próprio usuário. O endereço completo só aparece aqui; para os outros, só o bairro.</summary>
[ApiController]
[Route("api/me/places")]
[Authorize]
public class MyPlacesController(AppDbContext db, IOptions<GoogleMapsOptions> maps) : ControllerBase
{
    /// <summary>Atalho "Casa". 404 home_not_set na primeira vez: o app pede para definir.</summary>
    [HttpGet("home")]
    public async Task<IActionResult> GetHome(CancellationToken ct)
    {
        var home = await FindHomeAsync(ct);
        return home is null
            ? NotFound(new ApiError("home_not_set", "Você ainda não definiu sua casa."))
            : Ok(SavedPlaceDto.From(home));
    }

    [HttpPut("home")]
    public async Task<IActionResult> PutHome(SavePlaceRequest req, CancellationToken ct)
    {
        var address = req.Address?.Trim() ?? string.Empty;
        if (address.Length is 0 or > 300) return BadRequest(new ApiError("invalid_address", "Escolha o endereço da sua casa."));
        if (!Region.Contains(maps.Value, req.Latitude, req.Longitude))
            return BadRequest(new ApiError("outside_region", "O UniCar funciona só em Curitiba e região."));

        var home = await FindHomeAsync(ct);
        if (home is null)
        {
            home = new SavedPlace { UserId = User.UserId()!.Value, Kind = SavedPlaceKind.Home, Label = "Casa", Address = address };
            db.SavedPlaces.Add(home);
        }
        var label = req.Label?.Trim();
        home.Label = string.IsNullOrEmpty(label) ? "Casa" : label.Length > 60 ? label[..60] : label;
        home.Address = address;
        home.Neighborhood = string.IsNullOrWhiteSpace(req.Neighborhood) ? null : req.Neighborhood.Trim();
        home.GooglePlaceId = string.IsNullOrWhiteSpace(req.GooglePlaceId) ? null : req.GooglePlaceId.Trim();
        home.Latitude = req.Latitude;
        home.Longitude = req.Longitude;
        await db.SaveChangesAsync(ct);
        return Ok(SavedPlaceDto.From(home));
    }

    [HttpDelete("home")]
    public async Task<IActionResult> DeleteHome(CancellationToken ct)
    {
        var home = await FindHomeAsync(ct);
        if (home is not null)
        {
            db.SavedPlaces.Remove(home);
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    private Task<SavedPlace?> FindHomeAsync(CancellationToken ct)
    {
        var id = User.UserId();
        return db.SavedPlaces.FirstOrDefaultAsync(p => p.UserId == id && p.Kind == SavedPlaceKind.Home, ct);
    }
}
