using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using UniCar.Api.Auth;
using UniCar.Api.Common;
using UniCar.Api.Data;
using UniCar.Api.Driver;
using UniCar.Api.Rides;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(DemoCollection.Name)]
public class DemoSeedTests(DemoApiFactory api)
{
    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = api.CreateClient();
        var res = await client.PostAsJsonAsync("/api/dev/login", new { email });
        res.EnsureSuccessStatusCode();
        var auth = (await res.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Ana_tem_reserva_proxima_historico_e_casa()
    {
        var ana = await LoginAsync("ana.demo@ufpr.br");
        var trips = await ana.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        var next = Assert.Single(trips!.Upcoming);
        Assert.Equal(("Passenger", "Jardim das Américas"), (next.Kind, next.Destination));
        Assert.Equal(new[] { "Bruno" }, next.With);
        Assert.Equal(2, trips.History.Count);

        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/api/me/places/home")).StatusCode);
        Assert.NotEmpty((await ana.GetFromJsonAsync<List<RecentPlace>>("/api/me/places/recent"))!);
    }

    [Fact]
    public async Task Busca_de_amanha_para_o_Politecnico_tem_opcoes()
    {
        var ana = await LoginAsync("ana.demo@ufpr.br");
        var tomorrow = LocalTime.Today(TimeProvider.System).AddDays(1).ToString("yyyy-MM-dd");
        var rides = await ana.GetFromJsonAsync<List<RideDto>>(
            $"/api/rides/search?destLat=-25.4507&destLng=-49.2316&originLat=-25.4560&originLng=-49.2810&date={tomorrow}");
        Assert.True(rides!.Count >= 2);
        Assert.Contains(rides, r => r.ReservedByMe);
        Assert.All(rides, r => Assert.True(r.PricePerSeat >= 6m));
        // motoristas da demonstração já têm nota
        Assert.Equal(4.8, rides.First(r => r.Driver.FirstName == "Bruno").Driver.Rating);
        Assert.All(rides, r => Assert.NotNull(r.Driver.Rating));
    }

    [Fact]
    public async Task Admin_ve_Eduarda_em_analise_e_Carla_ve_a_sugestao()
    {
        var admin = await LoginAsync("admin.demo@ufpr.br");
        var pending = await admin.GetFromJsonAsync<List<AdminDriverDto>>("/api/admin/drivers");
        var eduarda = Assert.Single(pending!, p => p.Email == "eduarda.demo@up.edu.br");
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/admin/drivers/{eduarda.UserId}/selfie")).StatusCode);

        var carla = await LoginAsync("carla.demo@pucpr.edu.br");
        var trips = await carla.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        var ride = trips!.Upcoming.First(t => t.Kind == "Driver" && t.SeatsTaken == 2);
        var suggestions = await carla.GetFromJsonAsync<List<TimeSuggestionDto>>($"/api/rides/{ride.RideId}/time-suggestions");
        Assert.Equal(("Felipe", "Pending", 10), (Assert.Single(suggestions!).PassengerFirstName, suggestions![0].Status, suggestions[0].Minutes));
    }

    [Fact]
    public async Task Reset_recria_sem_duplicar_e_nao_aceita_conta_real()
    {
        var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/dev/demo/reset", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/dev/demo/reset", null)).StatusCode);
        await api.WithDbAsync(async db =>
            Assert.Equal(DemoSeed.Accounts.Length, await db.Users.CountAsync(u => u.Email.Contains(DemoSeed.Marker))));

        var res = await client.PostAsJsonAsync("/api/dev/login", new { email = "alguem@ufpr.br" });
        Assert.Equal("not_demo_account", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }
}

[Collection(ApiCollection.Name)]
public class DevRoutesOffTests(ApiFactory api)
{
    [Fact]
    public async Task Sem_Demo_Enabled_as_rotas_de_dev_nao_existem()
    {
        var client = api.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/dev/login", new { email = "ana.demo@ufpr.br" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/dev/demo/reset", null)).StatusCode);
        await api.WithDbAsync(async db => Assert.False(await db.Users.AnyAsync(u => u.Email.Contains(DemoSeed.Marker))));
    }
}
