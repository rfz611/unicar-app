using System.Net;
using System.Net.Http.Json;
using SkiaSharp;
using UniCar.Api.Common;
using UniCar.Api.Realtime;
using UniCar.Api.Rides;
using UniCar.Api.Trips;
using UniCar.Api.Users;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class RatingAndPhotoTests(ApiFactory api)
{
    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var res = await call;
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return (await res.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<string?> Error(Task<HttpResponseMessage> call) =>
        (await (await call).Content.ReadFromJsonAsync<ApiError>())?.Code;

    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.RoyalBlue);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    /// <summary>Pedir agora até o fim (motorista e passageira novos), com a nota que a passageira dá.</summary>
    private async Task<(HttpClient Driver, UserDto DriverUser, HttpClient Passenger, UserDto PassengerUser, TripDto Trip)> FinishedTripAsync(int stars)
    {
        var (driver, driverUser) = await api.NewDriverAsync();
        var (passenger, passengerUser) = await api.NewUserAsync();
        await driver.PostAsJsonAsync("/api/driver/online",
            new { latitude = -25.4420, longitude = -49.2890, destinationLatitude = -25.4507, destinationLongitude = -49.2316 });
        var request = await Ok<RideRequestDto>(passenger.PostAsJsonAsync("/api/ride-requests", new CreateRideRequestRequest(
            new PlaceInput("Água Verde", "Água Verde", -25.4560, -49.2810), RideTests.Politecnico)));
        var trip = await Ok<TripDto>(driver.PostAsync($"/api/ride-requests/{request.Id}/accept", null));
        foreach (var status in new[] { "DriverArrived", "InProgress", "Completed" })
            await driver.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status });
        await Ok<MyPaymentDto>(passenger.PostAsJsonAsync($"/api/trips/{trip.Id}/complete", new { paymentMethod = "Pix", rating = stars }));
        return (driver, driverUser, passenger, passengerUser, trip);
    }

    [Fact]
    public async Task Nota_aparece_no_perfil_na_busca_e_na_corrida()
    {
        var (driver, driverUser, passenger, passengerUser, trip) = await FinishedTripAsync(stars: 4);

        var profile = await Ok<PublicUserDto>(passenger.GetAsync($"/api/users/{driverUser.Id}"));
        Assert.Equal((4.0, 1, 1, true), (profile.Rating, profile.RatingsCount, profile.TripsCount, profile.IsDriver));

        // motorista avalia a passageira
        Assert.Equal(HttpStatusCode.NoContent,
            (await driver.PostAsJsonAsync($"/api/trips/{trip.Id}/ratings", new { toUserId = passengerUser.Id, stars = 5 })).StatusCode);
        Assert.Equal("already_rated", await Error(driver.PostAsJsonAsync($"/api/trips/{trip.Id}/ratings", new { toUserId = passengerUser.Id, stars = 3 })));
        var passengerProfile = await Ok<PublicUserDto>(driver.GetAsync($"/api/users/{passengerUser.Id}"));
        Assert.Equal((5.0, 1, 1, false), (passengerProfile.Rating, passengerProfile.RatingsCount, passengerProfile.TripsCount, passengerProfile.IsDriver));

        // passageira já avaliou ao pagar; nota para alguém de fora da corrida é recusada
        Assert.Equal("already_rated", await Error(passenger.PostAsJsonAsync($"/api/trips/{trip.Id}/ratings", new { toUserId = driverUser.Id, stars = 5 })));
        Assert.Equal("invalid_target", await Error(driver.PostAsJsonAsync($"/api/trips/{trip.Id}/ratings", new { toUserId = Guid.NewGuid(), stars = 5 })));

        // a nota do motorista aparece na corrida e na busca de caronas marcadas
        Assert.Equal(4.0, (await Ok<TripDto>(passenger.GetAsync($"/api/trips/{trip.Id}"))).Driver.Rating);
        var ride = await RideTests.PublishAsync(driver, RideTests.Tomorrow(time: "06:45"));
        var found = await Ok<List<RideDto>>(passenger.GetAsync(
            $"/api/rides/search?destLat=-25.4507&destLng=-49.2316&date={RideTests.Tomorrow().Date}"));
        var r = Assert.Single(found, x => x.Id == ride.Id);
        Assert.Equal((4.0, 1), (r.Driver.Rating, r.Driver.RatingsCount));
    }

    [Fact]
    public async Task Nao_avalia_antes_de_terminar()
    {
        var (driver, _) = await api.NewDriverAsync();
        var (passenger, passengerUser) = await api.NewUserAsync();
        await driver.PostAsJsonAsync("/api/driver/online",
            new { latitude = -25.4420, longitude = -49.2890, destinationLatitude = -25.4507, destinationLongitude = -49.2316 });
        var request = await Ok<RideRequestDto>(passenger.PostAsJsonAsync("/api/ride-requests", new CreateRideRequestRequest(
            new PlaceInput("Água Verde", "Água Verde", -25.4560, -49.2810), RideTests.Politecnico)));
        var trip = await Ok<TripDto>(driver.PostAsync($"/api/ride-requests/{request.Id}/accept", null));

        Assert.Equal("trip_not_finished", await Error(driver.PostAsJsonAsync($"/api/trips/{trip.Id}/ratings", new { toUserId = passengerUser.Id, stars = 5 })));
        await passenger.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status = "Cancelled" });
    }

    [Fact]
    public async Task Sem_avaliacoes_a_nota_e_nula()
    {
        var (client, user) = await api.NewUserAsync();
        var profile = await Ok<PublicUserDto>(client.GetAsync($"/api/users/{user.Id}"));
        Assert.Equal((null, 0, 0), (profile.Rating, profile.RatingsCount, profile.TripsCount));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/users/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact]
    public async Task Foto_vira_jpeg_512_quadrado_e_troca_apaga_a_antiga()
    {
        var (client, _) = await api.NewUserAsync();

        var me = await Ok<UserDto>(client.PostAsync("/api/users/me/photo", TestDriver.File(Png(800, 600))));
        Assert.StartsWith("/uploads/avatars/", me.PhotoUrl);
        var photo = await client.GetAsync(me.PhotoUrl);
        Assert.Equal("image/jpeg", photo.Content.Headers.ContentType?.MediaType);
        using (var decoded = SKBitmap.Decode(await photo.Content.ReadAsByteArrayAsync()))
            Assert.Equal((512, 512), (decoded.Width, decoded.Height));

        // trocar: a antiga some
        var again = await Ok<UserDto>(client.PostAsync("/api/users/me/photo", TestDriver.File(Png(100, 60))));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(me.PhotoUrl)).StatusCode);
        using (var small = SKBitmap.Decode(await (await client.GetAsync(again.PhotoUrl)).Content.ReadAsByteArrayAsync()))
            Assert.Equal((60, 60), (small.Width, small.Height)); // não amplia foto pequena

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/api/users/me/photo")).StatusCode);
        Assert.Null((await Ok<UserDto>(client.GetAsync("/api/users/me"))).PhotoUrl);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(again.PhotoUrl)).StatusCode);
    }

    [Fact]
    public async Task Foto_corrompida_ou_de_outro_tipo_e_recusada()
    {
        var (client, _) = await api.NewUserAsync();
        // começa como PNG mas o resto é lixo
        var broken = TestDriver.Png[..16].Concat(new byte[200]).ToArray();
        Assert.Equal("invalid_image", await Error(client.PostAsync("/api/users/me/photo", TestDriver.File(broken))));
        Assert.Equal("invalid_image", await Error(client.PostAsync("/api/users/me/photo", TestDriver.File("GIF89a..."u8.ToArray(), "a.gif"))));
    }

    [Fact]
    public async Task Avisos_de_carona_desligado_nao_recebe_o_me_avise()
    {
        var (quiet, _) = await api.NewUserAsync();
        var (loud, _) = await api.NewUserAsync();
        await quiet.PutAsJsonAsync("/api/users/me/preferences", new PreferencesDto("System", false, false, true));
        // destino perto do Prédio Histórico, longe dos outros testes
        foreach (var c in new[] { quiet, loud })
            await c.PostAsJsonAsync("/api/route-alerts", new { label = "Centro", latitude = -25.4297, longitude = -49.2611, radiusKm = 1 });
        await using var quietHub = await api.ConnectHubAsync(quiet);
        await using var loudHub = await api.ConnectHubAsync(loud);

        var loudGot = HubEvents.Next<RouteAlertDto>(loudHub, RealtimeEvents.RouteAlertMatched);
        var quietGot = HubEvents.Next<RouteAlertDto>(quietHub, RealtimeEvents.RouteAlertMatched, seconds: 3);
        var (driver, _) = await api.NewDriverAsync();
        await RideTests.PublishAsync(driver, RideTests.Tomorrow(dest: new PlaceInput("Praça Santos Andrade, 50 - Centro", "Centro", -25.4297, -49.2611)));

        await loudGot;
        await Assert.ThrowsAsync<TimeoutException>(() => quietGot);
        // mesmo sem aviso, o alerta fica marcado
        Assert.NotNull(Assert.Single((await Ok<List<RouteAlertDto>>(quiet.GetAsync("/api/route-alerts")))).MatchedRideId);
    }
}
