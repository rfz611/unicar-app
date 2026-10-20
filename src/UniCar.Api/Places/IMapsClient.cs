namespace UniCar.Api.Places;

public interface IMapsClient
{
    Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string input, string sessionToken, LatLng? near, CancellationToken ct = default);
    Task<PlaceDetails?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct = default);
    Task<PlaceDetails?> ReverseGeocodeAsync(LatLng point, CancellationToken ct = default);
    Task<RouteInfo?> ComputeRouteAsync(LatLng origin, LatLng destination, CancellationToken ct = default);
}
