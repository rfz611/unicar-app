using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.Api.Common;
using UniCar.Api.Domain;
using UniCar.Api.Pricing;
using UniCar.Api.Rides;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class RideTests(ApiFactory api)
{
    // Batel → UFPR Centro Politécnico; passageiro sai do Água Verde
    internal static readonly PlaceInput Batel = new("R. Bispo Dom José, 2000 - Batel, Curitiba", "Batel", -25.4420, -49.2890);
    internal static readonly PlaceInput Politecnico = new("Av. Cel. Francisco H. dos Santos, 100 - Jardim das Américas", "Jardim das Américas", -25.4507, -49.2316);
    private const double AguaVerdeLat = -25.4560, AguaVerdeLng = -49.2810;

    internal static CreateRideRequest Tomorrow(int seats = 3, string time = "08:00", PlaceInput? origin = null, PlaceInput? dest = null)
    {
        var tomorrow = LocalTime.Today(TimeProvider.System).AddDays(1).ToString("yyyy-MM-dd");
        return new CreateRideRequest(origin ?? Batel, dest ?? Politecnico, tomorrow, time, seats);
    }

    internal static CreateRideRequest InMinutes(int minutes)
    {
        var at = LocalTime.Now(TimeProvider.System).AddMinutes(minutes);
        return new CreateRideRequest(Batel, Politecnico, at.ToString("yyyy-MM-dd"), at.ToString("HH:mm"), 3);
    }

    internal static async Task<RideDto> PublishAsync(HttpClient driver, CreateRideRequest req)
    {
        var res = await driver.PostAsJsonAsync("/api/rides", req);
        if (res.StatusCode != HttpStatusCode.Created) throw new InvalidOperationException(await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<RideDto>())!;
    }

    private static string SearchUrl(double destLat, double destLng, string? date = null) =>
        FormattableString.Invariant($"/api/rides/search?destLat={destLat}&destLng={destLng}&originLat={AguaVerdeLat}&originLng={AguaVerdeLng}")
        + (date is null ? $"&date={Tomorrow().Date}" : $"&date={date}");

    [Fact]
    public async Task Motorista_publica_e_o_preco_vem_da_rota()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow());

        var route = await new FakeMapsClient().ComputeRouteAsync(new(Batel.Latitude, Batel.Longitude), new(Politecnico.Latitude, Politecnico.Longitude));
        var expected = new PricingService(Options.Create(new PricingOptions())).Quote(route!.Km, route.Minutes, RideMode.Scheduled);
        Assert.Equal(expected.Total, ride.PricePerSeat);
        Assert.Equal(("08:00", 3, 3, true), (ride.DepartureTime, ride.Seats, ride.SeatsLeft, ride.IsMine));
        Assert.Equal(Batel.Address, ride.Origin.Address); // o motorista vê o próprio endereço

        var estimate = await driver.PostAsJsonAsync("/api/rides/estimate", new EstimateRequest(Batel, Politecnico));
        Assert.Equal(expected.DriverAmount, (await estimate.Content.ReadFromJsonAsync<RideEstimateDto>())!.DriverAmountPerSeat);
    }

    [Fact]
    public async Task Quem_nao_e_motorista_nao_oferece()
    {
        var (client, _) = await api.NewUserAsync();
        var res = await client.PostAsJsonAsync("/api/rides", Tomorrow());
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("not_driver", (await res.Content.ReadFromJsonAsync<ApiError>())?.Code);
    }

    [Fact]
    public async Task Dados_invalidos_da_carona()
    {
        var (driver, _) = await api.NewDriverAsync();
        async Task<string?> Code(CreateRideRequest r) =>
            (await (await driver.PostAsJsonAsync("/api/rides", r)).Content.ReadFromJsonAsync<ApiError>())?.Code;

        Assert.Equal("invalid_seats", await Code(Tomorrow(seats: 5)));
        Assert.Equal("departure_too_soon", await Code(InMinutes(3)));
        Assert.Equal("outside_region", await Code(Tomorrow(dest: new PlaceInput("Av. Paulista", null, -23.56, -46.65))));
        Assert.Equal("same_place", await Code(Tomorrow(dest: Batel)));
        Assert.Equal("invalid_departure", await Code(Tomorrow() with { Time = "8h" }));
    }

    [Fact]
    public async Task Busca_acha_pelo_destino_e_esconde_a_saida_do_motorista()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow(time: "07:15"));
        var (passenger, _) = await api.NewUserAsync();

        // ~1 km do destino da carona: aparece
        var found = await passenger.GetFromJsonAsync<List<RideDto>>(SearchUrl(-25.4460, -49.2380));
        var r = Assert.Single(found!, x => x.Id == ride.Id);
        Assert.Null(r.Origin.Address);
        Assert.Null(r.Origin.Latitude);
        Assert.Equal("Batel", r.Origin.Label);
        Assert.Equal("Bruno", r.Driver.FirstName);
        Assert.Equal("Onix 1.0", r.Vehicle.Model);
        Assert.NotNull(r.Pickup);
        Assert.False(r.Pickup!.Estimated);
        Assert.True(r.Pickup.MinutesAfterDeparture > 0);
        Assert.True(r.DistanceToDestinationKm < 2);

        // longe (centro, ~4 km) ou em outro dia: não aparece
        Assert.DoesNotContain(await passenger.GetFromJsonAsync<List<RideDto>>(SearchUrl(-25.4297, -49.2711)) ?? new(), x => x.Id == ride.Id);
        var otherDay = LocalTime.Today(TimeProvider.System).AddDays(2).ToString("yyyy-MM-dd");
        Assert.DoesNotContain(await passenger.GetFromJsonAsync<List<RideDto>>(SearchUrl(-25.4507, -49.2316, otherDay)) ?? new(), x => x.Id == ride.Id);

        // o motorista não vê a própria carona na busca
        Assert.DoesNotContain(await driver.GetFromJsonAsync<List<RideDto>>(SearchUrl(-25.4507, -49.2316)) ?? new(), x => x.Id == ride.Id);
    }

    [Fact]
    public async Task Reservar_regras_basicas()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow(seats: 1));
        var (ana, _) = await api.NewUserAsync();
        var (bia, _) = await api.NewUserAsync();

        Assert.Equal(HttpStatusCode.Created, (await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null)).StatusCode);
        Assert.Equal("already_reserved", await ErrorAsync(ana.PostAsync($"/api/rides/{ride.Id}/reservations", null)));
        Assert.Equal("ride_full", await ErrorAsync(bia.PostAsync($"/api/rides/{ride.Id}/reservations", null)));
        Assert.Equal("own_ride", await ErrorAsync(driver.PostAsync($"/api/rides/{ride.Id}/reservations", null)));

        // quem reservou passa a ver o endereço de saída
        var detail = await ana.GetFromJsonAsync<RideDto>($"/api/rides/{ride.Id}");
        Assert.True(detail!.ReservedByMe);
        Assert.Equal(Batel.Address, detail.Origin.Address);

        var passengers = await driver.GetFromJsonAsync<List<PassengerDto>>($"/api/rides/{ride.Id}/passengers");
        Assert.Single(passengers!);
    }

    [Fact]
    public async Task Ultima_vaga_disputada_ao_mesmo_tempo_so_um_leva()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow(seats: 1));
        var passengers = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => api.NewUserAsync()));

        var results = await Task.WhenAll(passengers.Select(p => p.Client.PostAsync($"/api/rides/{ride.Id}/reservations", null)));

        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(results.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));
        await api.WithDbAsync(async db =>
        {
            var saved = await db.Rides.Include(r => r.Reservations).SingleAsync(r => r.Id == ride.Id);
            Assert.Equal(1, saved.SeatsTaken);
            Assert.Single(saved.Reservations, r => r.Status == ReservationStatus.Confirmed);
        });
    }

    [Fact]
    public async Task Cancelar_reserva_devolve_a_vaga_ate_30_min_antes()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow(seats: 1));
        var (ana, _) = await api.NewUserAsync();
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);

        Assert.Equal(HttpStatusCode.NoContent, (await ana.DeleteAsync($"/api/rides/{ride.Id}/reservations/mine")).StatusCode);
        Assert.Equal(1, (await ana.GetFromJsonAsync<RideDto>($"/api/rides/{ride.Id}"))!.SeatsLeft);

        var soon = await PublishAsync(driver, InMinutes(20));
        await ana.PostAsync($"/api/rides/{soon.Id}/reservations", null);
        Assert.Equal("too_late_to_cancel", await ErrorAsync(ana.DeleteAsync($"/api/rides/{soon.Id}/reservations/mine")));
    }

    [Fact]
    public async Task Nao_reserva_depois_da_saida()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow());
        await MoveDepartureAsync(ride.Id, TimeSpan.FromMinutes(-5));
        var (ana, _) = await api.NewUserAsync();
        Assert.Equal("ride_departed", await ErrorAsync(ana.PostAsync($"/api/rides/{ride.Id}/reservations", null)));
    }

    [Fact]
    public async Task Sugestao_de_horario_aceita_muda_a_saida()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow(time: "08:00"));
        var (ana, _) = await api.NewUserAsync();
        var (curioso, _) = await api.NewUserAsync();
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);

        Assert.Equal("not_passenger", await ErrorAsync(curioso.PostAsJsonAsync($"/api/rides/{ride.Id}/time-suggestions", new { minutes = 10 })));
        Assert.Equal("invalid_minutes", await ErrorAsync(ana.PostAsJsonAsync($"/api/rides/{ride.Id}/time-suggestions", new { minutes = 15 })));

        var res = await ana.PostAsJsonAsync($"/api/rides/{ride.Id}/time-suggestions", new { minutes = 10 });
        var s = (await res.Content.ReadFromJsonAsync<TimeSuggestionDto>())!;
        Assert.Equal(("08:00", "08:10", "Pending"), (s.CurrentDepartureTime, s.SuggestedDepartureTime, s.Status));
        Assert.Equal("suggestion_pending", await ErrorAsync(ana.PostAsJsonAsync($"/api/rides/{ride.Id}/time-suggestions", new { minutes = -10 })));

        // passageiro não aceita a própria sugestão
        Assert.Equal(HttpStatusCode.NotFound, (await ana.PostAsync($"/api/rides/{ride.Id}/time-suggestions/{s.Id}/accept", null)).StatusCode);

        var accepted = await driver.PostAsync($"/api/rides/{ride.Id}/time-suggestions/{s.Id}/accept", null);
        Assert.Equal("Accepted", (await accepted.Content.ReadFromJsonAsync<TimeSuggestionDto>())!.Status);
        Assert.Equal("08:10", (await ana.GetFromJsonAsync<RideDto>($"/api/rides/{ride.Id}"))!.DepartureTime);
    }

    [Fact]
    public async Task Aba_viagens_proximas_e_historico()
    {
        var (driver, _) = await api.NewDriverAsync(fullName: "Bruno Motorista");
        var ride = await PublishAsync(driver, Tomorrow(seats: 2));
        var (ana, _) = await api.NewUserAsync(fullName: "Ana Passageira");
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);

        var mine = await ana.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        var p = Assert.Single(mine!.Upcoming);
        Assert.Equal(("Passenger", "Confirmed", "Jardim das Américas", true), (p.Kind, p.Status, p.Destination, p.CanCancel));
        Assert.Equal(new[] { "Bruno" }, p.With);
        Assert.Equal(ride.PricePerSeat, p.Amount);

        var theirs = await driver.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        var d = Assert.Single(theirs!.Upcoming);
        Assert.Equal(("Driver", 1, 2), (d.Kind, d.SeatsTaken, d.Seats));
        Assert.Equal(new[] { "Ana" }, d.With);

        // depois que a viagem acontece, vai para o histórico
        await MoveDepartureAsync(ride.Id, TimeSpan.FromHours(-3));
        mine = await ana.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        Assert.Empty(mine!.Upcoming);
        Assert.Single(mine.History);
    }

    [Fact]
    public async Task Motorista_cancela_e_a_reserva_vai_para_o_historico()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow());
        var (ana, _) = await api.NewUserAsync();
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);

        Assert.Equal(HttpStatusCode.NoContent, (await driver.DeleteAsync($"/api/rides/{ride.Id}")).StatusCode);

        var trips = await ana.GetFromJsonAsync<MyTripsDto>("/api/me/trips");
        Assert.Empty(trips!.Upcoming);
        Assert.Equal("RideCancelled", Assert.Single(trips.History).Status);
        Assert.Equal("ride_cancelled", await ErrorAsync(ana.PostAsync($"/api/rides/{ride.Id}/reservations", null)));
    }

    [Fact]
    public async Task Recentes_e_alerta_me_avise()
    {
        var (ana, _) = await api.NewUserAsync();
        var alert = await ana.PostAsJsonAsync("/api/route-alerts", new { label = "UFPR Politécnico", latitude = -25.4510, longitude = -49.2320 });
        Assert.Equal(HttpStatusCode.Created, alert.StatusCode);

        var (driver, _) = await api.NewDriverAsync();
        var ride = await PublishAsync(driver, Tomorrow());

        var alerts = await ana.GetFromJsonAsync<List<RouteAlertDto>>("/api/route-alerts");
        Assert.Equal(ride.Id, Assert.Single(alerts!).MatchedRideId);

        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);
        var recent = await ana.GetFromJsonAsync<List<RecentPlace>>("/api/me/places/recent");
        Assert.Equal(Politecnico.Address, Assert.Single(recent!).Address);
    }

    private Task MoveDepartureAsync(Guid rideId, TimeSpan fromNow) => api.WithDbAsync(async db =>
    {
        var r = await db.Rides.SingleAsync(x => x.Id == rideId);
        r.DepartureAt = DateTimeOffset.UtcNow + fromNow;
        await db.SaveChangesAsync();
    });

    private static async Task<string?> ErrorAsync(Task<HttpResponseMessage> call) =>
        (await (await call).Content.ReadFromJsonAsync<ApiError>())?.Code;
}
