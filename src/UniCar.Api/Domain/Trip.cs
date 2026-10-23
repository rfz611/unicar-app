using System.ComponentModel.DataAnnotations;

namespace UniCar.Api.Domain;

public enum RideRequestStatus { Searching, Accepted, Cancelled, Expired }

/// <summary>"Pedir agora": o passageiro pede, motoristas dirigindo na mesma direção recebem, o primeiro que aceitar leva.</summary>
public class RideRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PassengerId { get; set; }
    public User? Passenger { get; set; }
    public required RidePlace Origin { get; set; }
    public required RidePlace Destination { get; set; }
    /// <summary>Preço do modo "Pedir agora" e a parte do motorista.</summary>
    public decimal Price { get; set; }
    public decimal DriverAmount { get; set; }
    public double Km { get; set; }
    public int Minutes { get; set; }
    public string? EncodedPolyline { get; set; }
    public RideRequestStatus Status { get; set; } = RideRequestStatus.Searching;
    public Guid? TripId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Sem motorista até aqui, o pedido expira.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>xmin: dois motoristas aceitando ao mesmo tempo, só um grava.</summary>
    [Timestamp]
    public uint Version { get; set; }
}

public enum TripMode { Now, Scheduled }

/// <summary>Accepted → DriverArrived → InProgress → Completed. Cancelled só antes de começar.</summary>
public enum TripStatus { Accepted, DriverArrived, InProgress, Completed, Cancelled }

/// <summary>Corrida acontecendo ou feita, dos dois modos: vem de um pedido (Now) ou de uma carona marcada (Scheduled).</summary>
public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public TripMode Mode { get; set; }
    public Guid? RideRequestId { get; set; }
    public Guid? RideId { get; set; }
    public Guid DriverId { get; set; }
    public User? Driver { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public required RidePlace Origin { get; set; }
    public required RidePlace Destination { get; set; }
    public double Km { get; set; }
    public int Minutes { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Accepted;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ArrivedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>Última posição enviada pelo motorista (hub UpdateLocation).</summary>
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTimeOffset? LastLocationAt { get; set; }

    public List<TripPassenger> Passengers { get; set; } = new();

    [Timestamp]
    public uint Version { get; set; }

    public bool IsActive => Status is TripStatus.Accepted or TripStatus.DriverArrived or TripStatus.InProgress;
}

public enum PaymentMethod { Pix, Card }

/// <summary>Open: ainda não pagou. PendingPayment: escolheu Pix/Cartão no fim (o pagamento real vem depois).</summary>
public enum PaymentStatus { Open, PendingPayment }

/// <summary>Cada passageiro da corrida, com o valor dele e o que escolheu na tela "Você chegou".</summary>
public class TripPassenger
{
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }
    public Guid PassengerId { get; set; }
    public User? Passenger { get; set; }
    public decimal Amount { get; set; }
    /// <summary>Parte do motorista nesse valor (o resto é a taxa do UniCar).</summary>
    public decimal DriverAmount { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Open;
    /// <summary>Nota que este passageiro deu ao motorista (também vira um <see cref="Rating"/>).</summary>
    public int? RatingGiven { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>Nota de 1 a 5 ao fim da corrida.</summary>
public class Rating
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public int Stars { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum MessageKind { Text, System }

/// <summary>Mensagem do chat. ConversationId = id da Trip ou da Ride. System = aviso automático (sem autor).</summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationId { get; set; }
    public Guid? AuthorId { get; set; }
    public User? Author { get; set; }
    public MessageKind Kind { get; set; } = MessageKind.Text;
    public required string Text { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Link público temporário "Compartilhar trajeto". Guardamos só o hash do token.</summary>
public class TripShare
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TripId { get; set; }
    public Trip? Trip { get; set; }
    public required string TokenHash { get; set; }
    public Guid CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}
