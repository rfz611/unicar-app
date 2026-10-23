using UniCar.Api.Domain;
using UniCar.Api.Driver;
using UniCar.Api.Rides;

namespace UniCar.Api.Trips;

/// <summary>"Pedir agora". Origem e destino como vieram de /api/places.</summary>
public record CreateRideRequestRequest(PlaceInput Origin, PlaceInput Destination);

public record RideRequestDto(Guid Id, string Status, RidePlaceDto Origin, RidePlaceDto Destination,
    decimal Price, double Km, int Minutes, string? EncodedPolyline, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt,
    Guid? TripId, int DriversNotified)
{
    public static RideRequestDto From(RideRequest r, int driversNotified = 0) => new(
        r.Id, r.Status.ToString(), RideMapper.Place(r.Origin, true), RideMapper.Place(r.Destination, true),
        r.Price, r.Km, r.Minutes, r.EncodedPolyline, r.CreatedAt, r.ExpiresAt, r.TripId, driversNotified);
}

/// <summary>O que o motorista vê ao receber um pedido: onde buscar, para onde, quanto recebe e o desvio.</summary>
public record RideRequestOfferDto(Guid RequestId, string PassengerFirstName, string PassengerUniversity,
    RidePlaceDto Origin, RidePlaceDto Destination, decimal DriverAmount, double Km, int Minutes,
    double PickupKm, double DetourKm, int DetourMinutes, DateTimeOffset ExpiresAt);

public record DriverOnlineRequest(double Latitude, double Longitude, double DestinationLatitude, double DestinationLongitude);

public record TripStatusRequest(string Status);

/// <summary>Tela "Você chegou": Pix ou Card, nota de 1 a 5 para o motorista.</summary>
public record CompleteTripRequest(string PaymentMethod, int? Rating);

public record LocationDto(double Latitude, double Longitude, DateTimeOffset At);

public record TripPassengerDto(Guid UserId, string FirstName, string? PhotoUrl);

public record MyPaymentDto(decimal Amount, string? Method, string Status, int? Rating);

/// <summary>
/// Corrida ao vivo. <see cref="EtaMinutes"/> é até o ponto de embarque (Accepted) ou até o destino (InProgress),
/// estimado a partir da última posição do motorista. <see cref="MyPayment"/> só para passageiro.
/// </summary>
public record TripDto(
    Guid Id, string Mode, string Status, Guid? RideId, Guid? RideRequestId, Guid ConversationId,
    RidePlaceDto Origin, RidePlaceDto Destination, double Km, int Minutes,
    RideDriverDto Driver, VehicleDto Vehicle, IReadOnlyList<TripPassengerDto> Passengers,
    LocationDto? DriverLocation, int? EtaMinutes, string? EtaTo,
    DateTimeOffset CreatedAt, DateTimeOffset? ArrivedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt, MyPaymentDto? MyPayment);

public record ShareDto(string Token, string Url, DateTimeOffset ExpiresAt);

/// <summary>Link público "Compartilhar trajeto": só posição e destino.</summary>
public record PublicTripDto(string Status, RidePlaceDto Destination, LocationDto? DriverLocation, int? EtaMinutes,
    DateTimeOffset ExpiresAt);
