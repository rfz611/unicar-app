using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Driver;
using UniCar.Api.Places;
using UniCar.Api.Realtime;
using UniCar.Api.Rides;

namespace UniCar.Api.Trips;

/// <summary>Carregar, mostrar e avisar sobre corridas; usado pelos controllers e pelo hub.</summary>
public class TripService(AppDbContext db, IRealtime realtime, DriverPresence presence, TimeProvider clock)
{
    public Task<Trip?> LoadAsync(Guid id, CancellationToken ct = default) =>
        db.Trips
            .Include(t => t.Driver).ThenInclude(d => d!.University)
            .Include(t => t.Driver).ThenInclude(d => d!.Preferences)
            .Include(t => t.Vehicle)
            .Include(t => t.Passengers).ThenInclude(p => p.Passenger)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public static IEnumerable<Guid> Participants(Trip t) => t.Passengers.Select(p => p.PassengerId).Append(t.DriverId);

    public static bool IsParticipant(Trip t, Guid userId) => Participants(t).Contains(userId);

    /// <summary>Chat da corrida: o da carona marcada (rideId) ou o da própria corrida (pedir agora).</summary>
    public static Guid ConversationId(Trip t) => t.RideId ?? t.Id;

    /// <summary>Corrida ativa (não terminada nem cancelada) do usuário, como motorista ou passageiro.</summary>
    public Task<bool> HasActiveTripAsync(Guid userId, CancellationToken ct = default) =>
        db.Trips.AnyAsync(t => (t.DriverId == userId || t.Passengers.Any(p => p.PassengerId == userId))
                               && (t.Status == TripStatus.Accepted || t.Status == TripStatus.DriverArrived
                                   || t.Status == TripStatus.InProgress), ct);

    public static TripDto ToDto(Trip t, Guid viewerId)
    {
        LocationDto? location = t.LastLatitude is null
            ? null
            : new LocationDto(t.LastLatitude.Value, t.LastLongitude!.Value, t.LastLocationAt!.Value);
        var (eta, etaTo) = Eta(t);
        var me = t.Passengers.FirstOrDefault(p => p.PassengerId == viewerId);

        return new TripDto(
            t.Id, t.Mode.ToString(), t.Status.ToString(), t.RideId, t.RideRequestId, ConversationId(t),
            RideMapper.Place(t.Origin, true), RideMapper.Place(t.Destination, true), t.Km, t.Minutes,
            RideMapper.Driver(t.Driver!), VehicleDto.From(t.Vehicle!),
            t.Passengers.Select(p => new TripPassengerDto(p.PassengerId, RideMapper.FirstName(p.Passenger!), p.Passenger!.PhotoUrl)).ToList(),
            location, eta, etaTo, t.CreatedAt, t.ArrivedAt, t.StartedAt, t.CompletedAt, t.CancelledAt,
            me is null ? null : new MyPaymentDto(me.Amount, me.PaymentMethod?.ToString(), me.PaymentStatus.ToString(), me.RatingGiven));
    }

    /// <summary>Minutos até o embarque (Accepted) ou até o destino (InProgress), pela última posição do motorista.</summary>
    public static (int? Minutes, string? To) Eta(Trip t)
    {
        if (t.LastLatitude is null) return (null, null);
        var from = new LatLng(t.LastLatitude.Value, t.LastLongitude!.Value);
        return t.Status switch
        {
            TripStatus.Accepted => ((int)Math.Ceiling(Geo.EstimateRoute(from, new(t.Origin.Latitude, t.Origin.Longitude)).Minutes), "pickup"),
            TripStatus.InProgress => ((int)Math.Ceiling(Geo.EstimateRoute(from, new(t.Destination.Latitude, t.Destination.Longitude)).Minutes), "destination"),
            _ => (null, null)
        };
    }

    public Task NotifyAsync(Trip t) => realtime.TripUpdatedAsync(Participants(t), ToDto(t, Guid.Empty));

    /// <summary>Hub UpdateLocation: só o motorista da corrida, só enquanto ela está ativa.</summary>
    public async Task<ApiError?> UpdateLocationAsync(Guid tripId, Guid driverId, double lat, double lng, CancellationToken ct)
    {
        if (lat is < -90 or > 90 || lng is < -180 or > 180) return new ApiError("invalid_location", "Posição inválida.");
        var trip = await db.Trips.Include(t => t.Passengers).FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is null || trip.DriverId != driverId) return new ApiError("trip_not_found", "Corrida não encontrada.");
        if (!trip.IsActive) return new ApiError("trip_not_active", "Essa corrida já terminou.");

        var now = clock.GetUtcNow();
        trip.LastLatitude = lat;
        trip.LastLongitude = lng;
        trip.LastLocationAt = now;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { /* status mudou ao mesmo tempo: a próxima posição grava */ }
        presence.UpdatePosition(driverId, lat, lng);

        await realtime.DriverLocationAsync(trip.Passengers.Select(p => p.PassengerId), new DriverLocationEvent(tripId, lat, lng, now));
        return null;
    }
}
