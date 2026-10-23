using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniCar.Api.Chat;
using UniCar.Api.Common;
using UniCar.Api.Realtime;
using UniCar.Api.Data;
using UniCar.Api.Domain;

namespace UniCar.Api.Rides;

/// <summary>Reservar lugar, cancelar a reserva e sugerir "10 min antes / depois".</summary>
[ApiController]
[Route("api/rides/{rideId:guid}")]
[Authorize]
public class ReservationsController(AppDbContext db, ChatService chat, IRealtime realtime, TimeProvider clock) : ControllerBase
{
    private const int MaxAttempts = 3;
    private Guid Me => User.UserId()!.Value;

    /// <summary>
    /// Reserva um lugar. Duas pessoas na última vaga ao mesmo tempo: o xmin da carona faz uma delas
    /// falhar no SaveChanges, ela tenta de novo e recebe "ride_full".
    /// </summary>
    [HttpPost("reservations")]
    public async Task<IActionResult> Reserve(Guid rideId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
            if (ride is null) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
            if (ride.Status == RideStatus.Cancelled) return Conflict(new ApiError("ride_cancelled", "Essa carona foi cancelada."));
            if (ride.DepartureAt <= clock.GetUtcNow()) return Conflict(new ApiError("ride_departed", "Essa carona já saiu."));
            if (ride.DriverId == Me) return Conflict(new ApiError("own_ride", "Você não pode reservar a sua própria carona."));
            if (await db.Reservations.AnyAsync(r => r.RideId == rideId && r.PassengerId == Me && r.Status == ReservationStatus.Confirmed, ct))
                return Conflict(new ApiError("already_reserved", "Você já reservou um lugar nessa carona."));
            if (ride.SeatsLeft <= 0) return Conflict(new ApiError("ride_full", "Não há mais vagas nessa carona."));

            ride.SeatsTaken++;
            var reservation = new Reservation { RideId = ride.Id, PassengerId = Me, Price = ride.PricePerSeat };
            db.Reservations.Add(reservation);
            try
            {
                await db.SaveChangesAsync(ct);
                return StatusCode(201, new { reservationId = reservation.Id, rideId, price = reservation.Price, seatsLeft = ride.SeatsLeft });
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear(); // alguém mexeu nas vagas: recarrega e confere de novo
            }
            catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Conflict(new ApiError("already_reserved", "Você já reservou um lugar nessa carona."));
            }
        }
        return Conflict(new ApiError("ride_full", "As vagas acabaram de esgotar."));
    }

    /// <summary>Cancela a minha reserva, até 30 min antes da saída.</summary>
    [HttpDelete("reservations/mine")]
    public async Task<IActionResult> CancelMine(Guid rideId, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var reservation = await db.Reservations.Include(r => r.Ride)
                .FirstOrDefaultAsync(r => r.RideId == rideId && r.PassengerId == Me && r.Status == ReservationStatus.Confirmed, ct);
            if (reservation is null) return NotFound(new ApiError("no_reservation", "Você não tem reserva nessa carona."));
            var now = clock.GetUtcNow();
            if (!RideRules.CanPassengerCancel(reservation.Ride!.DepartureAt, now))
                return Conflict(new ApiError("too_late_to_cancel", "Dá para cancelar só até 30 minutos antes da saída. Fale com o motorista pelo chat."));

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = now;
            reservation.Ride.SeatsTaken--;
            try
            {
                await db.SaveChangesAsync(ct);
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                db.ChangeTracker.Clear();
            }
        }
        return Conflict(new ApiError("try_again", "Não foi possível cancelar agora. Tente de novo."));
    }

    /// <summary>Passageiro com reserva sugere sair 10 min antes (-10) ou depois (+10). Uma pendente por vez.</summary>
    [HttpPost("time-suggestions")]
    public async Task<IActionResult> Suggest(Guid rideId, TimeSuggestionRequest req, CancellationToken ct)
    {
        if (!RideRules.SuggestionMinutes.Contains(req.Minutes))
            return BadRequest(new ApiError("invalid_minutes", "Sugira 10 minutos antes (-10) ou depois (10)."));

        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
        if (ride is null) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
        if (!await db.Reservations.AnyAsync(r => r.RideId == rideId && r.PassengerId == Me && r.Status == ReservationStatus.Confirmed, ct))
            return StatusCode(403, new ApiError("not_passenger", "Só quem reservou pode sugerir horário."));
        if (ride.Status != RideStatus.Scheduled || ride.DepartureAt.AddMinutes(req.Minutes) <= clock.GetUtcNow() + RideRules.MinLeadTime)
            return Conflict(new ApiError("too_late", "Não dá mais para mudar o horário dessa carona."));
        if (await db.TimeSuggestions.AnyAsync(s => s.RideId == rideId && s.PassengerId == Me && s.Status == SuggestionStatus.Pending, ct))
            return Conflict(new ApiError("suggestion_pending", "Você já tem uma sugestão esperando resposta."));

        var suggestion = new TimeSuggestion
        {
            RideId = rideId, PassengerId = Me, Minutes = req.Minutes, DepartureAtWhenSuggested = ride.DepartureAt
        };
        db.TimeSuggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);

        var dto = await DtoAsync(suggestion.Id, ct);
        var when = req.Minutes < 0 ? "10 min antes" : "10 min depois";
        await chat.SystemAsync(rideId, $"{dto.PassengerFirstName} sugeriu sair {when} ({dto.SuggestedDepartureTime}).", ct);
        await realtime.TimeSuggestedAsync(ride.DriverId, dto);
        return StatusCode(201, dto);
    }

    /// <summary>Motorista vê todas as sugestões da carona; passageiro, só as dele.</summary>
    [HttpGet("time-suggestions")]
    public async Task<IActionResult> ListSuggestions(Guid rideId, CancellationToken ct)
    {
        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == rideId, ct);
        if (ride is null) return NotFound(new ApiError("ride_not_found", "Carona não encontrada."));
        var isDriver = ride.DriverId == Me;
        var me = Me;
        var list = await Suggestions()
            .Where(s => s.RideId == rideId && (isDriver || s.PassengerId == me))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
        return Ok(list.Select(ToDto));
    }

    /// <summary>Motorista aceita: o horário da carona muda; as outras sugestões pendentes ficam recusadas.</summary>
    [HttpPost("time-suggestions/{suggestionId:guid}/accept")]
    public Task<IActionResult> Accept(Guid rideId, Guid suggestionId, CancellationToken ct) => AnswerAsync(rideId, suggestionId, true, ct);

    [HttpPost("time-suggestions/{suggestionId:guid}/decline")]
    public Task<IActionResult> Decline(Guid rideId, Guid suggestionId, CancellationToken ct) => AnswerAsync(rideId, suggestionId, false, ct);

    private async Task<IActionResult> AnswerAsync(Guid rideId, Guid suggestionId, bool accept, CancellationToken ct)
    {
        var s = await db.TimeSuggestions.Include(x => x.Ride)
            .FirstOrDefaultAsync(x => x.Id == suggestionId && x.RideId == rideId, ct);
        if (s is null || s.Ride!.DriverId != Me) return NotFound(new ApiError("suggestion_not_found", "Sugestão não encontrada."));
        if (s.Status != SuggestionStatus.Pending) return Conflict(new ApiError("already_answered", "Essa sugestão já foi respondida."));

        var now = clock.GetUtcNow();
        if (accept)
        {
            var newDeparture = s.DepartureAtWhenSuggested.AddMinutes(s.Minutes);
            if (s.Ride.Status != RideStatus.Scheduled || newDeparture <= now + RideRules.MinLeadTime)
                return Conflict(new ApiError("too_late", "Não dá mais para mudar o horário dessa carona."));
            s.Ride.DepartureAt = newDeparture;

            // as outras foram feitas em cima do horário antigo
            foreach (var other in await db.TimeSuggestions
                         .Where(x => x.RideId == rideId && x.Id != s.Id && x.Status == SuggestionStatus.Pending).ToListAsync(ct))
            {
                other.Status = SuggestionStatus.Declined;
                other.AnsweredAt = now;
            }
        }
        s.Status = accept ? SuggestionStatus.Accepted : SuggestionStatus.Declined;
        s.AnsweredAt = now;
        await db.SaveChangesAsync(ct);

        var dto = await DtoAsync(s.Id, ct);
        if (accept)
        {
            var time = RideMapper.Time(s.Ride.DepartureAt);
            await chat.SystemAsync(rideId, $"Horário alterado: a carona sai às {time}.", ct);
            var passengers = await db.Reservations
                .Where(r => r.RideId == rideId && r.Status == ReservationStatus.Confirmed).Select(r => r.PassengerId).ToListAsync(ct);
            await realtime.RideUpdatedAsync(passengers, new RideUpdatedEvent(
                rideId, s.Ride.Status.ToString(), s.Ride.DepartureAt, time, $"A carona agora sai às {time}."));
        }
        else
            await chat.SystemAsync(rideId, $"O motorista manteve o horário ({RideMapper.Time(s.Ride.DepartureAt)}).", ct);
        return Ok(dto);
    }

    private IQueryable<TimeSuggestion> Suggestions() => db.TimeSuggestions.Include(s => s.Passenger);

    private async Task<TimeSuggestionDto> DtoAsync(Guid id, CancellationToken ct) =>
        ToDto(await Suggestions().AsNoTracking().FirstAsync(s => s.Id == id, ct));

    private static TimeSuggestionDto ToDto(TimeSuggestion s) => new(
        s.Id, s.RideId, s.PassengerId, RideMapper.FirstName(s.Passenger!), s.Minutes, s.Status.ToString(),
        RideMapper.Time(s.DepartureAtWhenSuggested), RideMapper.Time(s.DepartureAtWhenSuggested.AddMinutes(s.Minutes)), s.CreatedAt);
}
