using UniCar.API.Models;
namespace UniCar.API.DTOs;
public record MapPoint(double Latitude, double Longitude, string Label, string? PlaceId = null);
public record RouteRequest(MapPoint Origin, MapPoint Destination, string VehicleType);
public record RouteQuoteDto(Guid Id, MapPoint Origin, MapPoint Destination, string VehicleType, int DistanceMeters, int DurationSeconds, string EncodedPolyline, DateTimeOffset ExpiresAt);
public record PricingSuggestionDto(decimal? Amount, string Currency, string? UnavailableReason, decimal? CostPerKm, int OriginalSeats, string ConfigurationVersion);
public record SuggestionRequest(int OriginalSeats, PricingMode PricingMode);
public record SubmitProposalRequest(int ExpectedVersion, decimal Amount);
public record VersionRequest(int ExpectedVersion);
public record ProposalDto(int Id, int AuthorId, string AuthorName, decimal Amount, DateTimeOffset CreatedAt);
public record RideSummaryDto(int Id, string Origin, string Destination, DateTime DepartureTime, string VehicleType, string DriverName, PricingMode PricingMode, decimal? ReferenceAmount, int OriginalSeats = 0);
public record NegotiationDto(int Id, int RideId, int PassengerId, int DriverId, string CounterpartyName, NegotiationStatus Status, int Version, decimal LatestAmount, int LatestAuthorId, bool CanRespond, bool CanWithdraw, RideSummaryDto RideSummary, IReadOnlyList<ProposalDto> Proposals, int? ReservationId);
public record ReservationReceiptDto(int ReservationId, int RideId, decimal? AgreedAmount, string Currency, PricingMode PricingMode);
public record PageResult<T>(IReadOnlyList<T> Items, int Page, bool HasMore);
public record PassengerAgreementDto(string Name, decimal? AgreedAmount, string Currency);
public record ActivityTripDto(RideSummaryDto RideSummary, string Role, DateTimeOffset? ReservedAt, decimal? AgreedAmount, IReadOnlyList<PassengerAgreementDto> Passengers);

public record ReferencePriceRequest(decimal ReferenceAmount);
