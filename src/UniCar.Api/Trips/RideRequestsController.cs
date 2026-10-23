using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Places;
using UniCar.Api.Pricing;
using UniCar.Api.Realtime;
using UniCar.Api.Rides;

namespace UniCar.Api.Trips;

/// <summary>"Pedir agora" (passageiro) e "dirigindo" (motorista).</summary>
[ApiController]
[Route("api")]
[Authorize]
public class RideRequestsController(
    AppDbContext db, RouteService routes, PricingService pricing, DriverPresence presence, IRealtime realtime,
    TripService trips, IOptions<GoogleMapsOptions> region, TimeProvider clock) : ControllerBase
{
    /// <summary>Sem motorista em 10 minutos, o pedido expira.</summary>
    public static readonly TimeSpan RequestLifetime = TimeSpan.FromMinutes(10);
    private Guid Me => User.UserId()!.Value;

    // ---------- Motorista ----------

    /// <summary>
    /// "Estou dirigindo": posição atual e destino. Fica online por 2 min; renove por aqui ou pelo hub
    /// (UpdateDriverPosition). Devolve os pedidos abertos que já combinam com o trajeto.
    /// </summary>
    [HttpPost("driver/online")]
    public async Task<IActionResult> Online(DriverOnlineRequest req, CancellationToken ct)
    {
        var error = await CheckDriverAsync(ct);
        if (error is not null) return StatusCode(403, error);
        if (!Region.Contains(region.Value, req.Latitude, req.Longitude) ||
            !Region.Contains(region.Value, req.DestinationLatitude, req.DestinationLongitude))
            return BadRequest(new ApiError("outside_region", "O UniCar funciona só em Curitiba e região."));

        var position = presence.SetOnline(Me, req.Latitude, req.Longitude, req.DestinationLatitude, req.DestinationLongitude);
        return Ok(await OffersForAsync(position, ct));
    }

    [HttpPost("driver/offline")]
    public IActionResult Offline()
    {
        presence.SetOffline(Me);
        return NoContent();
    }

    /// <summary>Pedidos abertos que combinam com o meu trajeto (para quem perdeu o evento RideRequested).</summary>
    [HttpGet("driver/ride-requests")]
    public async Task<IActionResult> OpenRequests(CancellationToken ct)
    {
        var position = presence.Get(Me);
        if (position is null) return Conflict(new ApiError("not_online", "Avise que está dirigindo (POST /api/driver/online)."));
        return Ok(await OffersForAsync(position, ct));
    }

    /// <summary>Motorista aceita. O primeiro leva; os outros recebem RideRequestClosed. Vira uma Trip.</summary>
    [HttpPost("ride-requests/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        var error = await CheckDriverAsync(ct);
        if (error is not null) return StatusCode(403, error);
        if (await trips.HasActiveTripAsync(Me, ct))
            return Conflict(new ApiError("busy", "Termine a corrida atual antes de aceitar outra."));

        var request = await db.RideRequests.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request is null) return NotFound(new ApiError("request_not_found", "Pedido não encontrado."));
        if (request.PassengerId == Me) return Conflict(new ApiError("own_request", "Você não pode aceitar o próprio pedido."));
        ExpireIfLate(request);
        if (request.Status != RideRequestStatus.Searching)
            return Conflict(new ApiError("already_taken", "Esse pedido não está mais disponível."));

        var vehicle = await db.Vehicles.FirstAsync(v => v.UserId == Me && v.Active, ct);
        var trip = new Trip
        {
            Mode = TripMode.Now, RideRequestId = request.Id, DriverId = Me, VehicleId = vehicle.Id,
            Origin = Copy(request.Origin), Destination = Copy(request.Destination), Km = request.Km, Minutes = request.Minutes,
            Passengers = { new TripPassenger { PassengerId = request.PassengerId, Amount = request.Price, DriverAmount = request.DriverAmount } }
        };
        var position = presence.Get(Me);
        if (position is not null)
        {
            trip.LastLatitude = position.Latitude;
            trip.LastLongitude = position.Longitude;
            trip.LastLocationAt = position.UpdatedAt;
        }
        request.Status = RideRequestStatus.Accepted;
        request.TripId = trip.Id;
        db.Trips.Add(trip);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiError("already_taken", "Outro motorista aceitou primeiro."));
        }

        presence.SetOffline(Me); // ocupado até terminar
        var others = presence.TakeOffers(request.Id).Where(d => d != Me);
        await realtime.RideRequestClosedAsync(others, new RideRequestClosedEvent(request.Id, "taken"));

        var loaded = (await trips.LoadAsync(trip.Id, ct))!;
        await trips.NotifyAsync(loaded);
        return StatusCode(201, TripService.ToDto(loaded, Me));
    }

    // ---------- Passageiro ----------

    /// <summary>"Procurando colegas": cria o pedido e avisa os motoristas dirigindo na mesma direção.</summary>
    [HttpPost("ride-requests")]
    public async Task<IActionResult> Create(CreateRideRequestRequest req, CancellationToken ct)
    {
        if (req.Origin is null || req.Destination is null)
            return BadRequest(new ApiError("invalid_place", "Escolha a saída e o destino."));
        if (!Region.Contains(region.Value, req.Origin.Latitude, req.Origin.Longitude) ||
            !Region.Contains(region.Value, req.Destination.Latitude, req.Destination.Longitude))
            return BadRequest(new ApiError("outside_region", "O UniCar funciona só em Curitiba e região."));
        if (Geo.Km(req.Origin.Latitude, req.Origin.Longitude, req.Destination.Latitude, req.Destination.Longitude) < 0.5)
            return BadRequest(new ApiError("same_place", "Saída e destino estão muito perto."));

        var now = clock.GetUtcNow();
        var me = Me;
        if (await db.RideRequests.AnyAsync(r => r.PassengerId == me && r.Status == RideRequestStatus.Searching && r.ExpiresAt > now, ct))
            return Conflict(new ApiError("request_open", "Você já tem um pedido procurando motorista."));
        if (await trips.HasActiveTripAsync(me, ct))
            return Conflict(new ApiError("busy", "Você já está numa corrida."));

        var (route, _) = await routes.GetAsync(
            new LatLng(req.Origin.Latitude, req.Origin.Longitude), new LatLng(req.Destination.Latitude, req.Destination.Longitude), ct);
        if (route is null) return BadRequest(new ApiError("no_route", "Não encontramos rota entre esses pontos."));
        var price = pricing.Quote(route.Km, route.Minutes, RideMode.Now);

        var request = new RideRequest
        {
            PassengerId = me, Origin = ToPlace(req.Origin), Destination = ToPlace(req.Destination),
            Price = price.Total, DriverAmount = price.DriverAmount, Km = price.Km, Minutes = price.Minutes,
            EncodedPolyline = route.EncodedPolyline, CreatedAt = now, ExpiresAt = now + RequestLifetime
        };
        db.RideRequests.Add(request);
        await db.SaveChangesAsync(ct);

        var notified = await OfferToDriversAsync(request, ct);
        return StatusCode(201, RideRequestDto.From(request, notified));
    }

    [HttpGet("ride-requests/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var request = await db.RideRequests.FirstOrDefaultAsync(r => r.Id == id && r.PassengerId == Me, ct);
        if (request is null) return NotFound(new ApiError("request_not_found", "Pedido não encontrado."));
        ExpireIfLate(request);
        return Ok(RideRequestDto.From(request));
    }

    /// <summary>Cancelar enquanto procura. Depois de aceito, cancele a corrida (POST /api/trips/{id}/status).</summary>
    [HttpDelete("ride-requests/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var request = await db.RideRequests.FirstOrDefaultAsync(r => r.Id == id && r.PassengerId == Me, ct);
        if (request is null) return NotFound(new ApiError("request_not_found", "Pedido não encontrado."));
        if (request.Status == RideRequestStatus.Accepted)
            return Conflict(new ApiError("already_accepted", "Um motorista já aceitou. Cancele pela corrida."));
        if (request.Status == RideRequestStatus.Searching)
        {
            request.Status = RideRequestStatus.Cancelled;
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateConcurrencyException) { return Conflict(new ApiError("already_accepted", "Um motorista acabou de aceitar.")); }
            await realtime.RideRequestClosedAsync(presence.TakeOffers(id), new RideRequestClosedEvent(id, "cancelled"));
        }
        return NoContent();
    }

    // ---------- Apoio ----------

    private async Task<int> OfferToDriversAsync(RideRequest request, CancellationToken ct)
    {
        var passenger = await db.Users.Include(u => u.University).FirstAsync(u => u.Id == request.PassengerId, ct);
        var busy = (await db.Trips
                .Where(t => t.Status == TripStatus.Accepted || t.Status == TripStatus.DriverArrived || t.Status == TripStatus.InProgress)
                .Select(t => t.DriverId).ToListAsync(ct))
            .ToHashSet();

        var count = 0;
        foreach (var driver in presence.Online().Where(d => d.DriverId != request.PassengerId && !busy.Contains(d.DriverId)))
        {
            var offer = Offer(request, passenger, driver);
            if (offer is null) continue;
            presence.RecordOffer(request.Id, driver.DriverId);
            await realtime.RideRequestedAsync(new[] { driver.DriverId }, offer);
            count++;
        }
        return count;
    }

    private async Task<List<RideRequestOfferDto>> OffersForAsync(DriverPosition driver, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var open = await db.RideRequests.Include(r => r.Passenger).ThenInclude(p => p!.University)
            .Where(r => r.Status == RideRequestStatus.Searching && r.ExpiresAt > now && r.PassengerId != driver.DriverId)
            .OrderBy(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);
        var offers = new List<RideRequestOfferDto>();
        foreach (var r in open)
        {
            var offer = Offer(r, r.Passenger!, driver);
            if (offer is null) continue;
            presence.RecordOffer(r.Id, driver.DriverId);
            offers.Add(offer);
        }
        return offers;
    }

    private static RideRequestOfferDto? Offer(RideRequest r, User passenger, DriverPosition d)
    {
        var detour = RideMatching.Compute(
            new LatLng(d.Latitude, d.Longitude), new LatLng(d.DestinationLatitude, d.DestinationLongitude),
            new LatLng(r.Origin.Latitude, r.Origin.Longitude), new LatLng(r.Destination.Latitude, r.Destination.Longitude));
        if (!detour.Fits) return null;
        return new RideRequestOfferDto(r.Id, RideMapper.FirstName(passenger), passenger.University?.ShortName ?? string.Empty,
            RideMapper.Place(r.Origin, true), RideMapper.Place(r.Destination, true), r.DriverAmount, r.Km, r.Minutes,
            detour.PickupKm, detour.ExtraKm, detour.ExtraMinutes, r.ExpiresAt);
    }

    private async Task<ApiError?> CheckDriverAsync(CancellationToken ct)
    {
        var me = Me;
        var ok = await db.Users.AnyAsync(u => u.Id == me && u.DriverStatus == DriverStatus.Approved, ct)
                 && await db.Vehicles.AnyAsync(v => v.UserId == me && v.Active, ct);
        return ok ? null : new ApiError("not_driver", "Só motorista verificado pode dirigir pelo UniCar.");
    }

    private void ExpireIfLate(RideRequest r)
    {
        if (r.Status == RideRequestStatus.Searching && r.ExpiresAt <= clock.GetUtcNow()) r.Status = RideRequestStatus.Expired;
    }

    private static RidePlace ToPlace(PlaceInput p) => new()
    {
        Address = p.Address.Trim().Length > 300 ? p.Address.Trim()[..300] : p.Address.Trim(),
        Neighborhood = string.IsNullOrWhiteSpace(p.Neighborhood) ? null : p.Neighborhood.Trim(),
        Latitude = p.Latitude, Longitude = p.Longitude
    };

    private static RidePlace Copy(RidePlace p) =>
        new() { Address = p.Address, Neighborhood = p.Neighborhood, Latitude = p.Latitude, Longitude = p.Longitude };
}
