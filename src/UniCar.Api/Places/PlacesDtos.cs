namespace UniCar.Api.Places;

/// <summary>Sugestão da busca, com o nome como aparece no Google Maps.</summary>
public record PlaceSuggestion(string PlaceId, string Name, string Address);

public record PlaceDetails(string PlaceId, string Name, string Address, string? Neighborhood, double Latitude, double Longitude);

public record RouteInfo(int DistanceMeters, int DurationSeconds, string? EncodedPolyline)
{
    public double Km => DistanceMeters / 1000.0;
    public double Minutes => DurationSeconds / 60.0;
}

public record LatLng(double Latitude, double Longitude);
