using System.Globalization;
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

namespace UniCar.Api.Rides;

/// <summary>Caronas marcadas: oferecer (motorista), buscar, ver, cancelar.</summary>
[ApiController]
[Route("api/rides")]
[Authorize]
public class RidesController(
    AppDbContext db, RouteService routes, PricingService pricing, IOptions<GoogleMapsOptions> region, TimeProvider clock,
    IRealtime realtime)
    : ControllerBase
{
    private Guid Me => User.UserId()!.Value;

    /// <summary>"Você recebe por passageiro R$": rota real e o valor do motorista na carona marcada.</summary>
    [HttpPost("estimate")]
    public async Task<IActionResult> Estimate(EstimateRequest req, CancellationToken ct)
    {
        var (route, estimated) = await routes.GetAsync(
            new LatLng(req.Origin.Latitude, req.Origin.Longitude), new LatLng(req.Destination.Latitude, req.Destination.Longitude), ct);
        if (route is null) return BadRequest(new ApiError("no_route", "Não encontramos rota entre esses pontos."));
        var q = pricing.Quote(route.Km, route.Minutes, RideMode.Scheduled);
        return Ok(new RideEstimateDto(q.Km, q.Minutes, q.Total, q.DriverAmount, route.EncodedPolyline, estimated));
    }

    /// <summary>Publicar carona. Só motorista aprovado. O preço vem da rota real, nunca do app.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateRideRequest req, CancellationToken ct)
    {
        var driver = await db.Users.Include(u => u.University).Include(u => u.Preferences).FirstAsync(u => u.Id == Me, ct);
        var vehicle = await db.Vehicles.FirstOrDefaultAsync(v => v.UserId == Me && v.Active, ct);
        if (driver.DriverStatus != DriverStatus.Approved || vehicle is null)
            return StatusCode(403, new ApiError("not_driver", "Para oferecer carona, vire motorista no seu perfil."));

        var error = RideRules.ValidateCreate(req, region.Value, clock.GetUtcNow(), out var departure);
        if (error is not null) return BadRequest(error);

        var origin = new LatLng(req.Origin.Latitude, req.Origin.Longitude);
        var destination = new LatLng(req.Destination.Latitude, req.Destination.Longitude);
        var (route, _) = await routes.GetAsync(origin, destination, ct);
        if (route is null) return BadRequest(new ApiError("no_route", "Não encontramos rota entre esses pontos."));
        var price = pricing.Quote(route.Km, route.Minutes, RideMode.Scheduled);

        var ride = new Ride
        {
            DriverId = Me, Driver = driver, VehicleId = vehicle.Id, Vehicle = vehicle,
            Origin = ToPlace(req.Origin), Destination = ToPlace(req.Destination),
            DepartureAt = departure, Seats = req.Seats,
            PricePerSeat = price.Total, DriverAmountPerSeat = price.DriverAmount,
            Km = price.Km, Minutes = price.Minutes, EncodedPolyline = route.EncodedPolyline
        };
        db.Rides.Add(ride);
        var matched = await MatchRouteAlertsAsync(ride, ct);
        await db.SaveChangesAsync(ct);
        foreach (var alert in matched)
            await realtime.RouteAlertMatchedAsync(alert.UserId, RouteAlertDto.From(alert));

        return CreatedAtAction(nameof(Get), new { id = ride.Id }, RideMapper.ToDto(ride, Me, reservedByMe: false));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var ride = await WithDetails(db.Rides).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ride is null) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
        var reserved = await db.Reservations.AnyAsync(
            r => r.RideId == id && r.PassengerId == Me && r.Status == ReservationStatus.Confirmed, ct);
        return Ok(RideMapper.ToDto(ride, Me, reserved));
    }

    /// <summary>
    /// "Caronas marcadas" para o destino: destino da carona a até 2 km, no dia escolhido (padrão: hoje),
    /// ainda não saiu, com vaga (ou já reservada por mim). Com a origem do passageiro, calcula "passa por você".
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] double destLat, [FromQuery] double destLng,
        [FromQuery] double? originLat, [FromQuery] double? originLng, [FromQuery] string? date, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        DateOnly day;
        if (string.IsNullOrEmpty(date)) day = LocalTime.Today(clock);
        else if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
            return BadRequest(new ApiError("invalid_date", "Data no formato AAAA-MM-DD."));

        var from = LocalTime.ToUtc(day, TimeOnly.MinValue);
        var to = LocalTime.ToUtc(day.AddDays(1), TimeOnly.MinValue);
        if (from < now) from = now;

        // pré-filtro por caixa (~2,2 km) no banco; distância exata depois
        const double dLat = 0.02, dLng = 0.022;
        var me = Me;
        var candidates = await WithDetails(db.Rides)
            .Where(r => r.Status == RideStatus.Scheduled && r.DriverId != me
                        && r.DepartureAt >= from && r.DepartureAt < to
                        && r.Destination.Latitude >= destLat - dLat && r.Destination.Latitude <= destLat + dLat
                        && r.Destination.Longitude >= destLng - dLng && r.Destination.Longitude <= destLng + dLng
                        && (r.SeatsTaken < r.Seats
                            || r.Reservations.Any(x => x.PassengerId == me && x.Status == ReservationStatus.Confirmed)))
            .OrderBy(r => r.DepartureAt)
            .Take(60)
            .ToListAsync(ct);

        var mine = (await db.Reservations
                .Where(x => x.PassengerId == me && x.Status == ReservationStatus.Confirmed)
                .Select(x => x.RideId).ToListAsync(ct))
            .ToHashSet();

        var matches = candidates
            .Select(r => (Ride: r, Km: Geo.Km(r.Destination.Latitude, r.Destination.Longitude, destLat, destLng)))
            .Where(x => x.Km <= RideRules.SearchRadiusKm)
            .Take(30)
            .ToList();

        var passenger = originLat is not null && originLng is not null ? new LatLng(originLat.Value, originLng.Value) : null;
        var pickups = passenger is null
            ? new PickupDto?[matches.Count]
            : await Task.WhenAll(matches.Select(m => PickupAsync(m.Ride, passenger, ct)));

        return Ok(matches.Select((m, i) => RideMapper.ToDto(m.Ride, me, mine.Contains(m.Ride.Id), pickups[i], m.Km)));
    }

    /// <summary>Motorista cancela a carona: todas as reservas são canceladas junto.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var ride = await db.Rides.Include(r => r.Reservations).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ride is null || ride.DriverId != Me) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
        if (ride.Status == RideStatus.Cancelled) return NoContent();
        var now = clock.GetUtcNow();
        if (ride.DepartureAt <= now) return Conflict(new ApiError("ride_departed", "Essa carona já saiu."));

        ride.Status = RideStatus.Cancelled;
        ride.CancelledAt = now;
        var passengers = new List<Guid>();
        foreach (var r in ride.Reservations.Where(r => r.Status == ReservationStatus.Confirmed))
        {
            r.Status = ReservationStatus.Cancelled;
            r.CancelledAt = now;
            passengers.Add(r.PassengerId);
        }
        ride.SeatsTaken = 0;
        await db.SaveChangesAsync(ct);
        await realtime.RideUpdatedAsync(passengers, new RideUpdatedEvent(
            ride.Id, ride.Status.ToString(), ride.DepartureAt, RideMapper.Time(ride.DepartureAt), "O motorista cancelou a carona."));
        return NoContent();
    }

    /// <summary>"Passageiros" da carona que ofereci.</summary>
    [HttpGet("{id:guid}/passengers")]
    public async Task<IActionResult> Passengers(Guid id, CancellationToken ct)
    {
        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (ride is null || ride.DriverId != Me) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));

        var list = await db.Reservations
            .Where(r => r.RideId == id && r.Status == ReservationStatus.Confirmed)
            .Include(r => r.Passenger).ThenInclude(p => p!.University)
            .Include(r => r.Passenger).ThenInclude(p => p!.Preferences)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);
        return Ok(list.Select(r => new PassengerDto(
            r.Id, r.PassengerId, RideMapper.FirstName(r.Passenger!),
            r.Passenger!.Preferences?.ShowCourse == false ? null : r.Passenger.Course,
            r.Passenger.University?.ShortName ?? string.Empty, r.Passenger.PhotoUrl, r.CreatedAt)));
    }

    internal static IQueryable<Ride> WithDetails(IQueryable<Ride> q) => q
        .Include(r => r.Driver).ThenInclude(d => d!.University)
        .Include(r => r.Driver).ThenInclude(d => d!.Preferences)
        .Include(r => r.Vehicle);

    private async Task<PickupDto> PickupAsync(Ride ride, LatLng passenger, CancellationToken ct)
    {
        var start = new LatLng(ride.Origin.Latitude, ride.Origin.Longitude);
        var (route, estimated) = await routes.GetOrEstimateAsync(start, passenger, ct);

        var minutes = (int)Math.Round(route.Minutes);
        var at = ride.DepartureAt.AddMinutes(minutes);
        return new PickupDto(at, RideMapper.Time(at), minutes, estimated);
    }

    /// <summary>Marca os alertas "Me avise" cujo destino fica perto do destino da carona nova.</summary>
    private async Task<List<RouteAlert>> MatchRouteAlertsAsync(Ride ride, CancellationToken ct)
    {
        var lat = ride.Destination.Latitude;
        var lng = ride.Destination.Longitude;
        var alerts = await db.RouteAlerts
            .Where(a => a.UserId != ride.DriverId
                        && a.Latitude >= lat - 0.1 && a.Latitude <= lat + 0.1
                        && a.Longitude >= lng - 0.1 && a.Longitude <= lng + 0.1)
            .ToListAsync(ct);
        var matched = alerts.Where(a => Geo.Km(a.Latitude, a.Longitude, lat, lng) <= a.RadiusKm).ToList();
        foreach (var a in matched)
        {
            a.MatchedRideId = ride.Id;
            a.MatchedAt = clock.GetUtcNow();
        }
        return matched;
    }

    private static RidePlace ToPlace(PlaceInput p) => new()
    {
        Address = p.Address.Trim().Length > 300 ? p.Address.Trim()[..300] : p.Address.Trim(),
        Neighborhood = string.IsNullOrWhiteSpace(p.Neighborhood) ? null : p.Neighborhood.Trim(),
        Latitude = p.Latitude,
        Longitude = p.Longitude
    };
}
