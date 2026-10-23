using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Chat;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Rides;

namespace UniCar.Api.Trips;

/// <summary>Corrida ao vivo: ver, mudar status, pagar e avaliar, compartilhar trajeto; iniciar carona marcada.</summary>
[ApiController]
[Route("api")]
[Authorize]
public class TripsController(AppDbContext db, TripService trips, ChatService chat, TimeProvider clock) : ControllerBase
{
    public static readonly TimeSpan ShareLifetime = TimeSpan.FromHours(4);
    /// <summary>Carona marcada pode começar de 30 min antes até 2 h depois do horário.</summary>
    public static readonly TimeSpan StartWindowBefore = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan StartWindowAfter = TimeSpan.FromHours(2);

    private Guid Me => User.UserId()!.Value;

    [HttpGet("trips/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var trip = await trips.LoadAsync(id, ct);
        if (trip is null || !TripService.IsParticipant(trip, Me)) return NotFound(new ApiError("trip_not_found", "Corrida não encontrada."));
        return Ok(await trips.DtoAsync(trip, Me, ct));
    }

    /// <summary>
    /// Motorista: DriverArrived ("Chegou") → InProgress ("Já embarquei") → Completed (chegada), nessa ordem.
    /// Cancelled: motorista ou passageiro, só antes de começar a viagem.
    /// </summary>
    [HttpPost("trips/{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, TripStatusRequest req, CancellationToken ct)
    {
        var trip = await trips.LoadAsync(id, ct);
        if (trip is null || !TripService.IsParticipant(trip, Me)) return NotFound(new ApiError("trip_not_found", "Corrida não encontrada."));
        if (!Enum.TryParse<TripStatus>(req.Status, ignoreCase: true, out var next) || !Enum.IsDefined(next))
            return BadRequest(new ApiError("invalid_status", "Status inválido."));

        var isDriver = trip.DriverId == Me;
        var allowed = (trip.Status, next) switch
        {
            (TripStatus.Accepted, TripStatus.DriverArrived) => isDriver,
            (TripStatus.DriverArrived, TripStatus.InProgress) => isDriver,
            (TripStatus.InProgress, TripStatus.Completed) => isDriver,
            (TripStatus.Accepted or TripStatus.DriverArrived, TripStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed)
            return Conflict(new ApiError("invalid_transition", $"Não dá para ir de {trip.Status} para {next}."));

        var now = clock.GetUtcNow();
        trip.Status = next;
        switch (next)
        {
            case TripStatus.DriverArrived: trip.ArrivedAt = now; break;
            case TripStatus.InProgress: trip.StartedAt = now; break;
            case TripStatus.Completed: trip.CompletedAt = now; break;
            case TripStatus.Cancelled: trip.CancelledAt = now; break;
        }
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiError("try_again", "A corrida mudou agora há pouco. Tente de novo.")); }

        var driverName = RideMapper.FirstName(trip.Driver!);
        var notice = next switch
        {
            TripStatus.DriverArrived => $"{driverName} chegou no ponto de embarque.",
            TripStatus.Cancelled => isDriver ? $"{driverName} cancelou a corrida." : "O passageiro cancelou a corrida.",
            _ => null
        };
        if (notice is not null) await chat.SystemAsync(TripService.ConversationId(trip), notice, ct);
        await trips.NotifyAsync(trip);
        return Ok(await trips.DtoAsync(trip, Me, ct));
    }

    /// <summary>
    /// "Você chegou": escolhe Pix ou Card e dá a nota (1–5). O pagamento real vem depois: aqui fica
    /// registrado valor + método com status PendingPayment.
    /// </summary>
    [HttpPost("trips/{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CompleteTripRequest req, CancellationToken ct)
    {
        var trip = await trips.LoadAsync(id, ct);
        var mine = trip?.Passengers.FirstOrDefault(p => p.PassengerId == Me);
        if (trip is null || mine is null) return NotFound(new ApiError("trip_not_found", "Corrida não encontrada."));
        if (!Enum.TryParse<PaymentMethod>(req.PaymentMethod, ignoreCase: true, out var method) || !Enum.IsDefined(method))
            return BadRequest(new ApiError("invalid_payment_method", "Escolha Pix ou Cartão."));
        if (req.Rating is < 1 or > 5) return BadRequest(new ApiError("invalid_rating", "A nota vai de 1 a 5."));
        if (mine.PaymentStatus != PaymentStatus.Open) return Conflict(new ApiError("already_paid", "Você já fechou essa corrida."));

        var now = clock.GetUtcNow();
        if (trip.Status == TripStatus.InProgress && trip.Mode == TripMode.Now)
        {
            // passageiro único chegou antes de o motorista marcar: fecha a corrida
            trip.Status = TripStatus.Completed;
            trip.CompletedAt = now;
        }
        else if (trip.Status != TripStatus.Completed)
            return Conflict(new ApiError("trip_not_finished", "A corrida ainda não terminou."));

        mine.PaymentMethod = method;
        mine.PaymentStatus = PaymentStatus.PendingPayment;
        mine.CompletedAt = now;
        mine.RatingGiven = req.Rating;
        if (req.Rating is { } stars)
            db.Ratings.Add(new Rating { TripId = trip.Id, FromUserId = Me, ToUserId = trip.DriverId, Stars = stars, CreatedAt = now });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ApiError("try_again", "A corrida mudou agora há pouco. Tente de novo.")); }

        await trips.NotifyAsync(trip);
        return Ok(new MyPaymentDto(mine.Amount, method.ToString(), mine.PaymentStatus.ToString(), mine.RatingGiven));
    }

    /// <summary>
    /// Avaliar alguém da corrida depois que ela terminou: motorista avalia passageiro, passageiro avalia motorista
    /// (se não deu nota ao pagar). Uma nota por pessoa por corrida.
    /// </summary>
    [HttpPost("trips/{id:guid}/ratings")]
    public async Task<IActionResult> Rate(Guid id, RateRequest req, CancellationToken ct)
    {
        var trip = await trips.LoadAsync(id, ct);
        if (trip is null || !TripService.IsParticipant(trip, Me)) return NotFound(new ApiError("trip_not_found", "Corrida não encontrada."));
        if (trip.Status != TripStatus.Completed) return Conflict(new ApiError("trip_not_finished", "A corrida ainda não terminou."));
        if (req.Stars is < 1 or > 5) return BadRequest(new ApiError("invalid_rating", "A nota vai de 1 a 5."));

        var iAmDriver = trip.DriverId == Me;
        var validTarget = iAmDriver
            ? trip.Passengers.Any(p => p.PassengerId == req.ToUserId)
            : req.ToUserId == trip.DriverId;
        if (!validTarget) return BadRequest(new ApiError("invalid_target", "Avalie o motorista ou um passageiro dessa corrida."));
        if (await db.Ratings.AnyAsync(r => r.TripId == id && r.FromUserId == Me && r.ToUserId == req.ToUserId, ct))
            return Conflict(new ApiError("already_rated", "Você já avaliou essa pessoa nessa corrida."));

        db.Ratings.Add(new Rating { TripId = id, FromUserId = Me, ToUserId = req.ToUserId, Stars = req.Stars, CreatedAt = clock.GetUtcNow() });
        var mine = trip.Passengers.FirstOrDefault(p => p.PassengerId == Me);
        if (mine is not null) mine.RatingGiven = req.Stars;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>"Compartilhar trajeto": link público por 4 h (ou até a corrida acabar), só com posição e destino.</summary>
    [HttpPost("trips/{id:guid}/share")]
    public async Task<IActionResult> Share(Guid id, CancellationToken ct)
    {
        var trip = await trips.LoadAsync(id, ct);
        if (trip is null || !TripService.IsParticipant(trip, Me)) return NotFound(new ApiError("trip_not_found", "Corrida não encontrada."));
        if (!trip.IsActive) return Conflict(new ApiError("trip_not_active", "Essa corrida já terminou."));

        var token = Base64Url(RandomNumberGenerator.GetBytes(24));
        var share = new TripShare { TripId = id, TokenHash = Hash(token), CreatedById = Me, ExpiresAt = clock.GetUtcNow() + ShareLifetime };
        db.TripShares.Add(share);
        await db.SaveChangesAsync(ct);
        return Ok(new ShareDto(token, $"{Request.Scheme}://{Request.Host}/api/public/trips/{token}", share.ExpiresAt));
    }

    /// <summary>O que o contato de confiança vê pelo link (sem login).</summary>
    [HttpGet("public/trips/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> Public(string token, CancellationToken ct)
    {
        var hash = Hash(token);
        var share = await db.TripShares.AsNoTracking().FirstOrDefaultAsync(s => s.TokenHash == hash, ct);
        var trip = share is null ? null : await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == share.TripId, ct);
        if (share is null || trip is null || share.ExpiresAt <= clock.GetUtcNow() || !trip.IsActive)
            return NotFound(new ApiError("share_expired", "Esse link expirou ou a viagem já terminou."));

        LocationDto? location = trip.LastLatitude is null
            ? null : new LocationDto(trip.LastLatitude.Value, trip.LastLongitude!.Value, trip.LastLocationAt!.Value);
        return Ok(new PublicTripDto(trip.Status.ToString(), RideMapper.Place(trip.Destination, true), location,
            TripService.Eta(trip).Minutes, share.ExpiresAt));
    }

    /// <summary>Motorista começa a carona marcada: vira uma Trip com os passageiros confirmados.</summary>
    [HttpPost("rides/{rideId:guid}/start")]
    public async Task<IActionResult> StartScheduled(Guid rideId, CancellationToken ct)
    {
        var ride = await db.Rides.Include(r => r.Reservations).FirstOrDefaultAsync(r => r.Id == rideId, ct);
        if (ride is null || ride.DriverId != Me) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
        if (ride.Status != RideStatus.Scheduled) return Conflict(new ApiError("ride_cancelled", "Essa carona foi cancelada."));
        var existing = await db.Trips.Where(t => t.RideId == rideId).Select(t => (Guid?)t.Id).FirstOrDefaultAsync(ct);
        if (existing is not null) return Conflict(new ApiError("already_started", "Essa carona já começou."));

        var now = clock.GetUtcNow();
        if (now < ride.DepartureAt - StartWindowBefore || now > ride.DepartureAt + StartWindowAfter)
            return Conflict(new ApiError("outside_start_window", "Dá para começar a partir de 30 minutos antes do horário."));
        var confirmed = ride.Reservations.Where(r => r.Status == ReservationStatus.Confirmed).ToList();
        if (confirmed.Count == 0) return Conflict(new ApiError("no_passengers", "Ninguém reservou essa carona."));
        if (await trips.HasActiveTripAsync(Me, ct)) return Conflict(new ApiError("busy", "Termine a corrida atual primeiro."));

        var trip = new Trip
        {
            Mode = TripMode.Scheduled, RideId = ride.Id, DriverId = Me, VehicleId = ride.VehicleId,
            Origin = new RidePlace { Address = ride.Origin.Address, Neighborhood = ride.Origin.Neighborhood, Latitude = ride.Origin.Latitude, Longitude = ride.Origin.Longitude },
            Destination = new RidePlace { Address = ride.Destination.Address, Neighborhood = ride.Destination.Neighborhood, Latitude = ride.Destination.Latitude, Longitude = ride.Destination.Longitude },
            Km = ride.Km, Minutes = ride.Minutes,
            Passengers = confirmed.Select(r => new TripPassenger
            {
                PassengerId = r.PassengerId, Amount = r.Price, DriverAmount = ride.DriverAmountPerSeat
            }).ToList()
        };
        db.Trips.Add(trip);
        await db.SaveChangesAsync(ct);

        var loaded = (await trips.LoadAsync(trip.Id, ct))!;
        await trips.NotifyAsync(loaded);
        await chat.SystemAsync(ride.Id, $"{RideMapper.FirstName(loaded.Driver!)} saiu. Acompanhe pelo mapa.", ct);
        return StatusCode(201, await trips.DtoAsync(loaded, Me, ct));
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
