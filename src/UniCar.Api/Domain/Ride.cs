using System.ComponentModel.DataAnnotations;

namespace UniCar.Api.Domain;

/// <summary>Ponto de saída ou chegada guardado junto da carona (cópia, não muda se o lugar salvo mudar).</summary>
public class RidePlace
{
    public required string Address { get; set; }
    /// <summary>O que estranhos veem da saída do motorista (pode ser a casa dele).</summary>
    public string? Neighborhood { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public enum RideStatus { Scheduled, Cancelled }

/// <summary>Carona marcada: o motorista publica saída, destino, horário e vagas.</summary>
public class Ride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DriverId { get; set; }
    public User? Driver { get; set; }
    public int VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public required RidePlace Origin { get; set; }
    public required RidePlace Destination { get; set; }
    public DateTimeOffset DepartureAt { get; set; }

    public int Seats { get; set; }
    /// <summary>Lugares reservados. Mudar este número altera a linha, e o xmin pega duas reservas ao mesmo tempo.</summary>
    public int SeatsTaken { get; set; }

    /// <summary>Preço por passageiro (modo Scheduled) e quanto disso fica com o motorista.</summary>
    public decimal PricePerSeat { get; set; }
    public decimal DriverAmountPerSeat { get; set; }
    public double Km { get; set; }
    public int Minutes { get; set; }
    public string? EncodedPolyline { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Scheduled;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>Coluna de sistema xmin do PostgreSQL, usada como token de concorrência.</summary>
    [Timestamp]
    public uint Version { get; set; }

    public List<Reservation> Reservations { get; set; } = new();

    public int SeatsLeft => Seats - SeatsTaken;
}

public enum ReservationStatus { Confirmed, Cancelled }

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RideId { get; set; }
    public Ride? Ride { get; set; }
    public Guid PassengerId { get; set; }
    public User? Passenger { get; set; }
    /// <summary>Preço no momento da reserva.</summary>
    public decimal Price { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CancelledAt { get; set; }
}

public enum SuggestionStatus { Pending, Accepted, Declined }

/// <summary>Passageiro sugere sair 10 min antes ou depois; o motorista aceita ou recusa.</summary>
public class TimeSuggestion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RideId { get; set; }
    public Ride? Ride { get; set; }
    public Guid PassengerId { get; set; }
    public User? Passenger { get; set; }
    public int Minutes { get; set; }
    /// <summary>Horário da carona quando a sugestão foi feita (a sugestão é relativa a ele).</summary>
    public DateTimeOffset DepartureAtWhenSuggested { get; set; }
    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AnsweredAt { get; set; }
}

/// <summary>"Ninguém indo para lá → Me avise": guarda o destino e marca quando surge carona.</summary>
public class RouteAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Label { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 2;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Última carona que bateu com o alerta (o app mostra; push/SignalR vem depois).</summary>
    public Guid? MatchedRideId { get; set; }
    public DateTimeOffset? MatchedAt { get; set; }
}
