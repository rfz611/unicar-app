using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Places;

namespace UniCar.Api.Rides;

/// <summary>Aba Viagens, lugares recentes e alertas "Me avise".</summary>
[ApiController]
[Route("api")]
[Authorize]
public class MyTripsController(AppDbContext db, TimeProvider clock) : ControllerBase
{
    private const int HistoryLimit = 50;
    private Guid Me => User.UserId()!.Value;

    /// <summary>
    /// Próximas (reservas e caronas que ofereci, ainda por acontecer) e Histórico (já passaram ou foram canceladas).
    /// O ponto azul da aba é upcoming.length > 0.
    /// </summary>
    [HttpGet("me/trips")]
    public async Task<IActionResult> Trips(CancellationToken ct)
    {
        var me = Me;
        var now = clock.GetUtcNow();

        var asPassenger = await db.Reservations
            .Where(r => r.PassengerId == me)
            .Include(r => r.Ride!).ThenInclude(x => x.Driver)
            .Include(r => r.Ride!).ThenInclude(x => x.Vehicle)
            .ToListAsync(ct);

        var asDriver = await db.Rides
            .Where(r => r.DriverId == me)
            .Include(r => r.Vehicle)
            .Include(r => r.Reservations.Where(x => x.Status == ReservationStatus.Confirmed)).ThenInclude(x => x.Passenger)
            .ToListAsync(ct);

        var items = asPassenger.Select(r => Passenger(r, now)).Concat(asDriver.Select(r => Driver(r, now))).ToList();
        var upcoming = items.Where(i => i.Upcoming).Select(i => i.Item).OrderBy(i => i.DepartureAt).ToList();
        var history = items.Where(i => !i.Upcoming).Select(i => i.Item).OrderByDescending(i => i.DepartureAt).Take(HistoryLimit).ToList();
        return Ok(new MyTripsDto(upcoming, history));
    }

    /// <summary>Últimos 5 destinos diferentes das minhas viagens (como passageiro ou motorista).</summary>
    [HttpGet("me/places/recent")]
    public async Task<IActionResult> Recent(CancellationToken ct)
    {
        var me = Me;
        var asPassenger = db.Reservations
            .Where(r => r.PassengerId == me && r.Status == ReservationStatus.Confirmed)
            .Select(r => new RecentPlace(r.Ride!.Destination.Address, r.Ride.Destination.Neighborhood,
                r.Ride.Destination.Latitude, r.Ride.Destination.Longitude, r.Ride.DepartureAt));
        var asDriver = db.Rides
            .Where(r => r.DriverId == me && r.Status == RideStatus.Scheduled)
            .Select(r => new RecentPlace(r.Destination.Address, r.Destination.Neighborhood,
                r.Destination.Latitude, r.Destination.Longitude, r.DepartureAt));

        var all = (await asPassenger.ToListAsync(ct)).Concat(await asDriver.ToListAsync(ct))
            .OrderByDescending(x => x.DepartureAt)
            .DistinctBy(x => x.Address)
            .Take(5);
        return Ok(all);
    }

    [HttpPost("route-alerts")]
    public async Task<IActionResult> CreateAlert(CreateRouteAlertRequest req, CancellationToken ct)
    {
        var label = req.Label?.Trim() ?? string.Empty;
        if (label.Length is 0 or > 300) return BadRequest(new ApiError("invalid_label", "Diga para onde você quer ir."));
        var radius = req.RadiusKm ?? RideRules.SearchRadiusKm;
        if (radius is < 0.5 or > 10) return BadRequest(new ApiError("invalid_radius", "Raio entre 0,5 e 10 km."));
        if (await db.RouteAlerts.CountAsync(a => a.UserId == Me, ct) >= 10)
            return Conflict(new ApiError("too_many_alerts", "Você já tem 10 alertas. Apague algum antes."));

        var alert = new RouteAlert { UserId = Me, Label = label, Latitude = req.Latitude, Longitude = req.Longitude, RadiusKm = radius };
        db.RouteAlerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return StatusCode(201, RouteAlertDto.From(alert));
    }

    [HttpGet("route-alerts")]
    public async Task<IActionResult> ListAlerts(CancellationToken ct) =>
        Ok((await db.RouteAlerts.Where(a => a.UserId == Me).OrderByDescending(a => a.CreatedAt).ToListAsync(ct))
            .Select(RouteAlertDto.From));

    [HttpDelete("route-alerts/{id:guid}")]
    public async Task<IActionResult> DeleteAlert(Guid id, CancellationToken ct)
    {
        var alert = await db.RouteAlerts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == Me, ct);
        if (alert is not null)
        {
            db.RouteAlerts.Remove(alert);
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    private static (bool Upcoming, TripItemDto Item) Passenger(Reservation r, DateTimeOffset now)
    {
        var ride = r.Ride!;
        var active = r.Status == ReservationStatus.Confirmed && ride.Status == RideStatus.Scheduled;
        var status = ride.Status == RideStatus.Cancelled ? "RideCancelled" : r.Status.ToString();
        var item = new TripItemDto(
            "Passenger", ride.Id, r.Id, status,
            ride.DepartureAt, RideMapper.Date(ride.DepartureAt), RideMapper.Time(ride.DepartureAt),
            RideMapper.ShortLabel(ride.Origin), RideMapper.ShortLabel(ride.Destination),
            r.Price, new[] { RideMapper.FirstName(ride.Driver!) },
            ride.SeatsTaken, ride.Seats, VehicleLabel(ride.Vehicle!),
            active && RideRules.CanPassengerCancel(ride.DepartureAt, now));
        return (active && !Finished(ride, now), item);
    }

    private static (bool Upcoming, TripItemDto Item) Driver(Ride ride, DateTimeOffset now)
    {
        var active = ride.Status == RideStatus.Scheduled;
        var passengers = ride.Reservations.OrderBy(x => x.CreatedAt).Select(x => RideMapper.FirstName(x.Passenger!)).ToList();
        var item = new TripItemDto(
            "Driver", ride.Id, null, ride.Status.ToString(),
            ride.DepartureAt, RideMapper.Date(ride.DepartureAt), RideMapper.Time(ride.DepartureAt),
            RideMapper.ShortLabel(ride.Origin), RideMapper.ShortLabel(ride.Destination),
            ride.DriverAmountPerSeat * ride.SeatsTaken, passengers,
            ride.SeatsTaken, ride.Seats, VehicleLabel(ride.Vehicle!),
            active && ride.DepartureAt > now);
        return (active && !Finished(ride, now), item);
    }

    /// <summary>Considera a viagem feita quando passou a saída + duração da rota.</summary>
    private static bool Finished(Ride ride, DateTimeOffset now) => ride.DepartureAt.AddMinutes(ride.Minutes) < now;

    private static string VehicleLabel(Vehicle v) => $"{v.Model} {v.Color.ToLowerInvariant()} · {Plate.Display(v.Plate)}";
}
