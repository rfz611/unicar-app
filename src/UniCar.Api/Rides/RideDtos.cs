using UniCar.Api.Common;
using UniCar.Api.Domain;
using UniCar.Api.Driver;

namespace UniCar.Api.Rides;

/// <summary>Lugar como o app recebeu de GET /api/places/{id} ou /reverse.</summary>
public record PlaceInput(string Address, string? Neighborhood, double Latitude, double Longitude);

/// <summary>Tela "Oferecer". date = "AAAA-MM-DD" e time = "HH:MM", no horário de Curitiba.</summary>
public record CreateRideRequest(PlaceInput Origin, PlaceInput Destination, string Date, string Time, int Seats);

public record EstimateRequest(PlaceInput Origin, PlaceInput Destination);

/// <summary>"Você recebe por passageiro R$": <see cref="DriverAmountPerSeat"/>.</summary>
public record RideEstimateDto(double Km, int Minutes, decimal PricePerSeat, decimal DriverAmountPerSeat, string? EncodedPolyline,
    bool Estimated = false);

/// <summary>
/// Saída da carona. Para quem não está na carona, só o bairro (pode ser a casa do motorista):
/// <see cref="Address"/>, <see cref="Latitude"/> e <see cref="Longitude"/> vêm nulos.
/// </summary>
public record RidePlaceDto(string Label, string? Address, string? Neighborhood, double? Latitude, double? Longitude);

public record RideDriverDto(Guid Id, string FirstName, string? Course, string University, string? PhotoUrl,
    double? Rating, int RatingsCount);

/// <summary>"Passa por você em X min": horário estimado em que o carro passa pela origem do passageiro.</summary>
public record PickupDto(DateTimeOffset At, string Time, int MinutesAfterDeparture, bool Estimated);

public record RideDto(
    Guid Id, string Status,
    DateTimeOffset DepartureAt, string DepartureDate, string DepartureTime,
    RidePlaceDto Origin, RidePlaceDto Destination,
    decimal PricePerSeat, int Seats, int SeatsLeft, double Km, int Minutes,
    RideDriverDto Driver, VehicleDto Vehicle,
    PickupDto? Pickup, double? DistanceToDestinationKm,
    bool IsMine, bool ReservedByMe);

public record PassengerDto(Guid ReservationId, Guid UserId, string FirstName, string? Course, string University,
    string? PhotoUrl, DateTimeOffset ReservedAt);

public record TimeSuggestionRequest(int Minutes);

public record TimeSuggestionDto(Guid Id, Guid RideId, Guid PassengerId, string PassengerFirstName, int Minutes, string Status,
    string CurrentDepartureTime, string SuggestedDepartureTime, DateTimeOffset CreatedAt);

/// <summary>
/// Item da aba Viagens. <see cref="Kind"/>: Passenger ou Driver. <see cref="Mode"/>: Scheduled (carona marcada,
/// tem RideId) ou Now (pedir agora). <see cref="TripId"/>: corrida ao vivo, quando já começou (GET /api/trips/{id}).
/// </summary>
public record TripItemDto(
    string Kind, string Mode, Guid? RideId, Guid? TripId, Guid? ReservationId, string Status,
    DateTimeOffset DepartureAt, string DepartureDate, string DepartureTime,
    string Origin, string Destination,
    decimal Amount,              // passageiro: quanto paga; motorista: quanto recebe pelos lugares reservados
    IReadOnlyList<string> With,  // passageiro: o motorista; motorista: os passageiros
    int SeatsTaken, int Seats, string Vehicle, bool CanCancel);

public record MyTripsDto(IReadOnlyList<TripItemDto> Upcoming, IReadOnlyList<TripItemDto> History);

/// <summary>Item de "Recentes": destino de uma viagem minha.</summary>
public record RecentPlace(string Address, string? Neighborhood, double Latitude, double Longitude, DateTimeOffset DepartureAt);

public record CreateRouteAlertRequest(string Label, double Latitude, double Longitude, double? RadiusKm);

public record RouteAlertDto(Guid Id, string Label, double Latitude, double Longitude, double RadiusKm,
    Guid? MatchedRideId, DateTimeOffset? MatchedAt, DateTimeOffset CreatedAt)
{
    public static RouteAlertDto From(RouteAlert a) =>
        new(a.Id, a.Label, a.Latitude, a.Longitude, a.RadiusKm, a.MatchedRideId, a.MatchedAt, a.CreatedAt);
}

public static class RideMapper
{
    public static string Date(DateTimeOffset utc) => LocalTime.ToLocal(utc).ToString("yyyy-MM-dd");
    public static string Time(DateTimeOffset utc) => LocalTime.ToLocal(utc).ToString("HH:mm");

    /// <summary>Precisa de Driver (com University e Preferences) e Vehicle carregados.</summary>
    public static RideDto ToDto(Ride r, Guid viewerId, bool reservedByMe, PickupDto? pickup = null, double? distanceToDestinationKm = null)
    {
        var isMine = r.DriverId == viewerId;
        var participant = isMine || reservedByMe;
        return new RideDto(
            r.Id, r.Status.ToString(), r.DepartureAt, Date(r.DepartureAt), Time(r.DepartureAt),
            Place(r.Origin, participant), Place(r.Destination, true),
            r.PricePerSeat, r.Seats, r.SeatsLeft, r.Km, r.Minutes,
            Driver(r.Driver!), VehicleDto.From(r.Vehicle!),
            pickup, distanceToDestinationKm is null ? null : Math.Round(distanceToDestinationKm.Value, 1),
            isMine, reservedByMe);
    }

    public static RidePlaceDto Place(RidePlace p, bool full) => full
        ? new RidePlaceDto(p.Address, p.Address, p.Neighborhood, p.Latitude, p.Longitude)
        : new RidePlaceDto(p.Neighborhood ?? "Bairro não informado", null, p.Neighborhood, null, null);

    public static RideDriverDto Driver(User u) => new(
        u.Id, FirstName(u), u.Preferences?.ShowCourse == false ? null : u.Course,
        u.University?.ShortName ?? string.Empty, u.PhotoUrl, null, 0);

    public static string FirstName(User u) => u.FullName.Split(' ')[0];

    /// <summary>Rótulo curto para listas: bairro, senão o começo do endereço.</summary>
    public static string ShortLabel(RidePlace p) => p.Neighborhood ?? p.Address.Split(',')[0];
}
