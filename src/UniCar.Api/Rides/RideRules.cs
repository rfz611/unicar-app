using System.Globalization;
using UniCar.Api.Common;
using UniCar.Api.Places;

namespace UniCar.Api.Rides;

/// <summary>Regras das caronas marcadas, separadas do controller para dar para testar sem banco.</summary>
public static class RideRules
{
    public const int MaxSeats = 4;
    public static readonly TimeSpan MinLeadTime = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MaxAdvance = TimeSpan.FromDays(30);
    /// <summary>Passageiro cancela a reserva até 30 min antes da saída.</summary>
    public static readonly TimeSpan CancelDeadline = TimeSpan.FromMinutes(30);
    /// <summary>Destino da carona a até 2 km do destino buscado.</summary>
    public const double SearchRadiusKm = 2;
    public static readonly int[] SuggestionMinutes = { -10, 10 };

    public static ApiError? ValidateCreate(CreateRideRequest req, GoogleMapsOptions region, DateTimeOffset nowUtc,
        out DateTimeOffset departureUtc)
    {
        departureUtc = default;
        if (req.Origin is null || req.Destination is null)
            return new ApiError("invalid_place", "Escolha a saída e o destino.");
        if (string.IsNullOrWhiteSpace(req.Origin.Address) || string.IsNullOrWhiteSpace(req.Destination.Address))
            return new ApiError("invalid_place", "Escolha a saída e o destino.");
        if (!Region.Contains(region, req.Origin.Latitude, req.Origin.Longitude) ||
            !Region.Contains(region, req.Destination.Latitude, req.Destination.Longitude))
            return new ApiError("outside_region", "O UniCar funciona só em Curitiba e região.");
        if (Geo.Km(req.Origin.Latitude, req.Origin.Longitude, req.Destination.Latitude, req.Destination.Longitude) < 0.5)
            return new ApiError("same_place", "Saída e destino estão muito perto.");

        if (!DateOnly.TryParseExact(req.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ||
            !TimeOnly.TryParseExact(req.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return new ApiError("invalid_departure", "Escolha o dia e o horário da saída.");
        departureUtc = LocalTime.ToUtc(date, time);
        if (departureUtc < nowUtc + MinLeadTime)
            return new ApiError("departure_too_soon", "A saída precisa ser daqui a pelo menos 10 minutos.");
        if (departureUtc > nowUtc + MaxAdvance)
            return new ApiError("departure_too_far", "Dá para marcar caronas até 30 dias antes.");

        if (req.Seats is < 1 or > MaxSeats) return new ApiError("invalid_seats", "Escolha de 1 a 4 vagas.");
        return null;
    }

    public static bool CanPassengerCancel(DateTimeOffset departureUtc, DateTimeOffset nowUtc) =>
        departureUtc - nowUtc >= CancelDeadline;
}
