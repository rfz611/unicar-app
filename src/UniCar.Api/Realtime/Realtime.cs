using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;
using UniCar.Api.Chat;
using UniCar.Api.Rides;
using UniCar.Api.Trips;

namespace UniCar.Api.Realtime;

/// <summary>Nomes dos eventos que o app escuta em /hubs/trips (connection.on("...")).</summary>
public static class RealtimeEvents
{
    public const string RideRequested = "RideRequested";
    public const string RideRequestClosed = "RideRequestClosed";
    public const string TripUpdated = "TripUpdated";
    public const string DriverLocation = "DriverLocation";
    public const string MessageReceived = "MessageReceived";
    public const string Typing = "Typing";
    public const string RideUpdated = "RideUpdated";
    public const string TimeSuggested = "TimeSuggested";
    public const string RouteAlertMatched = "RouteAlertMatched";
}

public record RideRequestClosedEvent(Guid RequestId, string Reason);
public record DriverLocationEvent(Guid TripId, double Latitude, double Longitude, DateTimeOffset At);
public record TypingEvent(Guid ConversationId, Guid UserId, string FirstName);
/// <summary>Carona marcada mudou de horário ou foi cancelada. <see cref="Message"/> já vem pronta para mostrar.</summary>
public record RideUpdatedEvent(Guid RideId, string Status, DateTimeOffset DepartureAt, string DepartureTime, string Message);

/// <summary>Avisos em tempo real. Cada usuário recebe na conexão dele (id = claim "sub").</summary>
public interface IRealtime
{
    Task RideRequestedAsync(IEnumerable<Guid> drivers, RideRequestOfferDto offer);
    Task RideRequestClosedAsync(IEnumerable<Guid> drivers, RideRequestClosedEvent e);
    Task TripUpdatedAsync(IEnumerable<Guid> users, TripDto trip);
    Task DriverLocationAsync(IEnumerable<Guid> users, DriverLocationEvent e);
    Task MessageReceivedAsync(IEnumerable<Guid> users, MessageDto message);
    Task TypingAsync(IEnumerable<Guid> users, TypingEvent e);
    Task RideUpdatedAsync(IEnumerable<Guid> users, RideUpdatedEvent e);
    Task TimeSuggestedAsync(Guid driver, TimeSuggestionDto suggestion);
    Task RouteAlertMatchedAsync(Guid user, RouteAlertDto alert);
}

public class SignalRRealtime(IHubContext<TripsHub> hub) : IRealtime
{
    private Task Send(IEnumerable<Guid> users, string name, object payload)
    {
        var ids = users.Distinct().Select(u => u.ToString()).ToList();
        return ids.Count == 0 ? Task.CompletedTask : hub.Clients.Users(ids).SendAsync(name, payload);
    }

    public Task RideRequestedAsync(IEnumerable<Guid> drivers, RideRequestOfferDto offer) => Send(drivers, RealtimeEvents.RideRequested, offer);
    public Task RideRequestClosedAsync(IEnumerable<Guid> drivers, RideRequestClosedEvent e) => Send(drivers, RealtimeEvents.RideRequestClosed, e);
    public Task TripUpdatedAsync(IEnumerable<Guid> users, TripDto trip) => Send(users, RealtimeEvents.TripUpdated, trip);
    public Task DriverLocationAsync(IEnumerable<Guid> users, DriverLocationEvent e) => Send(users, RealtimeEvents.DriverLocation, e);
    public Task MessageReceivedAsync(IEnumerable<Guid> users, MessageDto message) => Send(users, RealtimeEvents.MessageReceived, message);
    public Task TypingAsync(IEnumerable<Guid> users, TypingEvent e) => Send(users, RealtimeEvents.Typing, e);
    public Task RideUpdatedAsync(IEnumerable<Guid> users, RideUpdatedEvent e) => Send(users, RealtimeEvents.RideUpdated, e);
    public Task TimeSuggestedAsync(Guid driver, TimeSuggestionDto s) => Send(new[] { driver }, RealtimeEvents.TimeSuggested, s);
    public Task RouteAlertMatchedAsync(Guid user, RouteAlertDto alert) => Send(new[] { user }, RealtimeEvents.RouteAlertMatched, alert);
}

/// <summary>Identifica a conexão SignalR pelo id do usuário (claim "sub" do token de acesso).</summary>
public class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
