using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UniCar.Api.Chat;
using UniCar.Api.Common;
using UniCar.Api.Trips;

namespace UniCar.Api.Realtime;

/// <summary>
/// /hubs/trips — conecte com o token de acesso (?access_token=... ou header Authorization).
/// O app chama os métodos abaixo e escuta os eventos de <see cref="RealtimeEvents"/>.
/// </summary>
[Authorize]
public class TripsHub(TripService trips, ChatService chat, DriverPresence presence) : Hub
{
    private Guid Me => Context.User!.UserId() ?? throw new HubException("Entre de novo.");

    /// <summary>Motorista em corrida: manda a posição a cada ~3 s. Os passageiros recebem DriverLocation.</summary>
    public async Task UpdateLocation(Guid tripId, double latitude, double longitude)
    {
        var error = await trips.UpdateLocationAsync(tripId, Me, latitude, longitude, Context.ConnectionAborted);
        if (error is not null) throw new HubException(error.Message);
    }

    /// <summary>Motorista online sem corrida: mantém a posição em dia para receber pedidos próximos.</summary>
    public Task UpdateDriverPosition(double latitude, double longitude)
    {
        presence.UpdatePosition(Me, latitude, longitude);
        return Task.CompletedTask;
    }

    /// <summary>Mensagem no chat da viagem (conversationId = id da Trip ou da Ride). Todos recebem MessageReceived.</summary>
    public async Task<MessageDto> SendMessage(Guid conversationId, string text)
    {
        var (message, error) = await chat.SendAsync(conversationId, Me, text, Context.ConnectionAborted);
        if (error is not null) throw new HubException(error.Message);
        return message!;
    }

    /// <summary>"digitando…": os outros participantes recebem Typing.</summary>
    public async Task Typing(Guid conversationId)
    {
        var error = await chat.TypingAsync(conversationId, Me, Context.ConnectionAborted);
        if (error is not null) throw new HubException(error.Message);
    }
}
