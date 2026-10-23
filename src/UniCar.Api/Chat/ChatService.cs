using Microsoft.EntityFrameworkCore;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Domain;
using UniCar.Api.Realtime;

namespace UniCar.Api.Chat;

public record MessageDto(Guid Id, Guid ConversationId, Guid? AuthorId, string? AuthorFirstName, string Kind, string Text,
    DateTimeOffset CreatedAt)
{
    public static MessageDto From(Message m) => new(
        m.Id, m.ConversationId, m.AuthorId, m.Author?.FullName.Split(' ')[0], m.Kind.ToString(), m.Text, m.CreatedAt);
}

public record SendMessageRequest(string Text);

/// <summary>
/// Chat por viagem: conversationId = id da Trip (pedir agora / corrida) ou da Ride (carona marcada).
/// Só participam o motorista e os passageiros confirmados.
/// </summary>
public class ChatService(AppDbContext db, IRealtime realtime)
{
    public const int MaxLength = 500;

    public static readonly string[] QuickReplies =
    {
        "Estou chegando!", "Já estou no ponto.", "Pode me esperar 2 minutinhos?", "Estou de blusa azul.", "Obrigado pela carona!"
    };

    /// <summary>Participantes da conversa; null se ela não existe.</summary>
    public async Task<HashSet<Guid>?> ParticipantsAsync(Guid conversationId, CancellationToken ct = default)
    {
        // corrida de "pedir agora" (a de carona marcada conversa pelo id da Ride)
        var trip = await db.Trips.AsNoTracking()
            .Where(t => t.Id == conversationId && t.RideId == null)
            .Select(t => new { t.DriverId, Passengers = t.Passengers.Select(p => p.PassengerId).ToList() })
            .FirstOrDefaultAsync(ct);
        if (trip is not null) return trip.Passengers.Append(trip.DriverId).ToHashSet();

        var ride = await db.Rides.AsNoTracking()
            .Where(r => r.Id == conversationId)
            .Select(r => new
            {
                r.DriverId,
                Passengers = r.Reservations.Where(x => x.Status == ReservationStatus.Confirmed).Select(x => x.PassengerId).ToList()
            })
            .FirstOrDefaultAsync(ct);
        return ride?.Passengers.Append(ride.DriverId).ToHashSet();
    }

    public async Task<(MessageDto? Message, ApiError? Error)> SendAsync(Guid conversationId, Guid authorId, string? text,
        CancellationToken ct = default)
    {
        var clean = text?.Trim() ?? string.Empty;
        if (clean.Length == 0) return (null, new ApiError("empty_message", "Escreva a mensagem."));
        if (clean.Length > MaxLength) return (null, new ApiError("message_too_long", "A mensagem tem no máximo 500 caracteres."));

        var participants = await ParticipantsAsync(conversationId, ct);
        if (participants is null) return (null, new ApiError("conversation_not_found", "Conversa não encontrada."));
        if (!participants.Contains(authorId)) return (null, new ApiError("not_participant", "Você não está nessa viagem."));

        var message = new Message { ConversationId = conversationId, AuthorId = authorId, Text = clean };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        await db.Entry(message).Reference(m => m.Author).LoadAsync(ct);

        var dto = MessageDto.From(message);
        await realtime.MessageReceivedAsync(participants, dto);
        return (dto, null);
    }

    /// <summary>Aviso automático no chat ("Felipe sugeriu sair 10 min depois", "O motorista chegou").</summary>
    public async Task SystemAsync(Guid conversationId, string text, CancellationToken ct = default)
    {
        var participants = await ParticipantsAsync(conversationId, ct);
        if (participants is null) return;
        var message = new Message { ConversationId = conversationId, Kind = MessageKind.System, Text = text };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        await realtime.MessageReceivedAsync(participants, MessageDto.From(message));
    }

    public async Task<ApiError?> TypingAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        var participants = await ParticipantsAsync(conversationId, ct);
        if (participants is null || !participants.Contains(userId)) return new ApiError("not_participant", "Você não está nessa viagem.");
        var name = await db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstAsync(ct);
        participants.Remove(userId);
        await realtime.TypingAsync(participants, new TypingEvent(conversationId, userId, name.Split(' ')[0]));
        return null;
    }
}
