using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using UniCar.Mobile.Models;

namespace UniCar.Mobile.Services;

public sealed class RideService(HttpClient http)
{
    public Task<RideResult<List<Ride>>> SearchAsync(string destination, string? vehicleType = null, DateTime? date = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(destination)) query.Add("destination=" + Uri.EscapeDataString(destination.Trim()));
        if (!string.IsNullOrWhiteSpace(vehicleType)) query.Add("vehicleType=" + Uri.EscapeDataString(vehicleType));
        if (date.HasValue) query.Add("date=" + date.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        return SendAsync<List<Ride>>(new HttpRequestMessage(HttpMethod.Get, "api/rides" + (query.Count > 0 ? "?" + string.Join("&", query) : "")));
    }

    public Task<RideResult<List<Ride>>> ReservationsAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/rides/reservations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return SendAsync<List<Ride>>(request);
    }

    public Task<RideResult<JsonElement>> JoinAsync(int id, string token)
        => SendAsync<JsonElement>(AuthorizedPost($"api/rides/{id}/join", token));

    public Task<RideResult<JsonElement>> CreateAsync(CreateRideRequest ride, string token)
    {
        var request = AuthorizedPost("api/rides", token);
        request.Content = JsonContent.Create(ride);
        return SendAsync<JsonElement>(request);
    }

    public Task<RideResult<JsonElement>> UpdateAsync(int id,CreateRideRequest ride,string token)
    {
        var request=AuthorizedPost($"api/rides/{id}/pricing-route",token);request.Method=HttpMethod.Patch;request.Content=JsonContent.Create(ride);return SendAsync<JsonElement>(request);
    }

    public Task<RideResult<JsonElement>> UpdateReferenceAsync(int id,decimal amount,string token)
    {
        var request=AuthorizedPost($"api/rides/{id}/reference",token);request.Method=HttpMethod.Patch;request.Content=JsonContent.Create(new{referenceAmount=amount});return SendAsync<JsonElement>(request);
    }

    private static HttpRequestMessage AuthorizedPost(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<RideResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        using (request)
        try
        {
            using var response = await http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return data is null
                    ? new(false, default, "Resposta inesperada do servidor.")
                    : new(true, data, null);
            }
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new(false, default, "Sua sessão expirou. Saia e entre novamente.");
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.NotFound)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (body.ValueKind == JsonValueKind.Object && body.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String)
                    return new(false, default, message.GetString());
            }
            return new(false, default, "Não foi possível concluir. Tente novamente.");
        }
        catch (JsonException) { return new(false, default, "Resposta inesperada do servidor."); }
        catch (HttpRequestException) { return new(false, default, "Não foi possível conectar. Verifique sua conexão e tente novamente."); }
        catch (TaskCanceledException) { return new(false, default, "O servidor demorou para responder. Tente novamente."); }
    }
}
