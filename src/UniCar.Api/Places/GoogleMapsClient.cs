using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace UniCar.Api.Places;

/// <summary>Sem GoogleMaps:ApiKey. A API responde 503 em vez de 500 (ver Program.cs).</summary>
public class MapsNotConfiguredException() : InvalidOperationException("Configure GoogleMaps:ApiKey (veja o README).");

/// <summary>
/// Conversa com o Google: Places API (New) para busca e detalhes, Geocoding para o endereço do GPS
/// e Routes API para distância e tempo reais pelas ruas.
/// </summary>
public class GoogleMapsClient(HttpClient http, IOptions<GoogleMapsOptions> opt, ILogger<GoogleMapsClient> log) : IMapsClient
{
    private readonly GoogleMapsOptions _o = opt.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(string input, string sessionToken, LatLng? near, CancellationToken ct = default)
    {
        var body = new
        {
            input,
            languageCode = "pt-BR",
            regionCode = "br",
            includedRegionCodes = new[] { "br" },
            sessionToken,
            locationRestriction = new
            {
                rectangle = new
                {
                    low = new { latitude = _o.MinLatitude, longitude = _o.MinLongitude },
                    high = new { latitude = _o.MaxLatitude, longitude = _o.MaxLongitude }
                }
            },
            origin = near is null ? null : new { latitude = near.Latitude, longitude = near.Longitude }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://places.googleapis.com/v1/places:autocomplete")
        {
            Content = JsonContent.Create(body, options: Json)
        };
        req.Headers.Add("X-Goog-Api-Key", _o.ApiKey);
        req.Headers.Add("X-Goog-FieldMask",
            "suggestions.placePrediction.placeId,suggestions.placePrediction.structuredFormat");

        var doc = await SendAsync(req, ct);
        if (doc is null || !doc.RootElement.TryGetProperty("suggestions", out var list)) return Array.Empty<PlaceSuggestion>();

        var result = new List<PlaceSuggestion>();
        foreach (var s in list.EnumerateArray())
        {
            if (!s.TryGetProperty("placePrediction", out var p)) continue;
            var id = p.GetProperty("placeId").GetString()!;
            var fmt = p.GetProperty("structuredFormat");
            var main = fmt.TryGetProperty("mainText", out var m) ? m.GetProperty("text").GetString() : null;
            var secondary = fmt.TryGetProperty("secondaryText", out var sec) ? sec.GetProperty("text").GetString() : null;
            result.Add(new PlaceSuggestion(id, main ?? string.Empty, secondary ?? string.Empty));
        }
        return result;
    }

    public async Task<PlaceDetails?> GetPlaceAsync(string placeId, string? sessionToken, CancellationToken ct = default)
    {
        var url = $"https://places.googleapis.com/v1/places/{Uri.EscapeDataString(placeId)}?languageCode=pt-BR&regionCode=br";
        if (!string.IsNullOrEmpty(sessionToken)) url += "&sessionToken=" + Uri.EscapeDataString(sessionToken);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("X-Goog-Api-Key", _o.ApiKey);
        req.Headers.Add("X-Goog-FieldMask", "id,displayName,formattedAddress,location,addressComponents");

        var doc = await SendAsync(req, ct);
        if (doc is null) return null;
        var r = doc.RootElement;
        var loc = r.GetProperty("location");
        return new PlaceDetails(
            r.GetProperty("id").GetString()!,
            r.TryGetProperty("displayName", out var dn) ? dn.GetProperty("text").GetString() ?? string.Empty : string.Empty,
            r.TryGetProperty("formattedAddress", out var fa) ? fa.GetString() ?? string.Empty : string.Empty,
            Neighborhood(r, "addressComponents", "types", "longText"),
            loc.GetProperty("latitude").GetDouble(),
            loc.GetProperty("longitude").GetDouble());
    }

    public async Task<PlaceDetails?> ReverseGeocodeAsync(LatLng point, CancellationToken ct = default)
    {
        var url = string.Format(CultureInfo.InvariantCulture,
            "https://maps.googleapis.com/maps/api/geocode/json?latlng={0},{1}&language=pt-BR&key={2}",
            point.Latitude, point.Longitude, Uri.EscapeDataString(_o.ApiKey));
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var doc = await SendAsync(req, ct);
        if (doc is null || !doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0) return null;

        var first = results[0];
        var address = first.GetProperty("formatted_address").GetString() ?? string.Empty;
        var shortAddress = address.Split(',').Take(2).Select(x => x.Trim());
        return new PlaceDetails(
            first.GetProperty("place_id").GetString()!,
            string.Join(", ", shortAddress),
            address,
            Neighborhood(first, "address_components", "types", "long_name"),
            point.Latitude, point.Longitude);
    }

    public async Task<RouteInfo?> ComputeRouteAsync(LatLng origin, LatLng destination, CancellationToken ct = default)
    {
        var body = new
        {
            origin = new { location = new { latLng = new { latitude = origin.Latitude, longitude = origin.Longitude } } },
            destination = new { location = new { latLng = new { latitude = destination.Latitude, longitude = destination.Longitude } } },
            travelMode = "DRIVE",
            routingPreference = "TRAFFIC_AWARE",
            languageCode = "pt-BR",
            units = "METRIC"
        };
        using var req = new HttpRequestMessage(HttpMethod.Post, "https://routes.googleapis.com/directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(body, options: Json)
        };
        req.Headers.Add("X-Goog-Api-Key", _o.ApiKey);
        req.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline");

        var doc = await SendAsync(req, ct);
        if (doc is null || !doc.RootElement.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0) return null;
        var route = routes[0];
        var seconds = int.Parse(route.GetProperty("duration").GetString()!.TrimEnd('s'), CultureInfo.InvariantCulture);
        var polyline = route.TryGetProperty("polyline", out var pl) ? pl.GetProperty("encodedPolyline").GetString() : null;
        return new RouteInfo(route.GetProperty("distanceMeters").GetInt32(), seconds, polyline);
    }

    private async Task<JsonDocument?> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_o.ApiKey))
            throw new MapsNotConfiguredException();

        using var res = await http.SendAsync(req, ct);
        var text = await res.Content.ReadAsStringAsync(ct);
        if (!res.IsSuccessStatusCode)
        {
            log.LogError("Google respondeu {Status}: {Body}", (int)res.StatusCode, text);
            return null;
        }
        return JsonDocument.Parse(text);
    }

    private static string? Neighborhood(JsonElement root, string componentsName, string typesName, string textName)
    {
        if (!root.TryGetProperty(componentsName, out var comps)) return null;
        foreach (var wanted in new[] { "sublocality_level_1", "sublocality", "neighborhood" })
        {
            foreach (var c in comps.EnumerateArray())
            {
                if (c.GetProperty(typesName).EnumerateArray().Any(t => t.GetString() == wanted))
                    return c.GetProperty(textName).GetString();
            }
        }
        return null;
    }
}
