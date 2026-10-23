using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using UniCar.Api.Chat;
using UniCar.Api.Common;
using UniCar.Api.Realtime;
using UniCar.Api.Rides;
using UniCar.Api.Trips;
using UniCar.Tests.Infra;
using Xunit;

namespace UniCar.Tests;

[Collection(ApiCollection.Name)]
public class RealtimeTests(ApiFactory api)
{
    // Motorista no Batel indo para o Politécnico; passageira no Água Verde indo para o Politécnico (desvio ~1 km)
    private static readonly object DriverBatelToPolitecnico =
        new { latitude = -25.4420, longitude = -49.2890, destinationLatitude = -25.4507, destinationLongitude = -49.2316 };
    private static readonly CreateRideRequestRequest AguaVerdeToPolitecnico = new(
        new PlaceInput("Av. República Argentina, 1500 - Água Verde", "Água Verde", -25.4560, -49.2810), RideTests.Politecnico);

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var res = await call;
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"{(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
        return (await res.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<string?> Error(Task<HttpResponseMessage> call) =>
        (await (await call).Content.ReadFromJsonAsync<ApiError>())?.Code;

    [Fact]
    public async Task Pedir_agora_do_pedido_ate_pagar()
    {
        var (driver, _) = await api.NewDriverAsync(fullName: "Bruno Lima");
        var (ana, _) = await api.NewUserAsync(fullName: "Ana Ribeiro");
        await using var driverHub = await api.ConnectHubAsync(driver);
        await using var anaHub = await api.ConnectHubAsync(ana);
        Assert.Equal(HttpStatusCode.OK, (await driver.PostAsJsonAsync("/api/driver/online", DriverBatelToPolitecnico)).StatusCode);

        // pedido: o motorista recebe pelo SignalR
        var offered = HubEvents.Next<RideRequestOfferDto>(driverHub, RealtimeEvents.RideRequested);
        var request = await Ok<RideRequestDto>(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico));
        Assert.Equal("Searching", request.Status);
        Assert.True(request.DriversNotified >= 1);
        var offer = await offered;
        Assert.Equal((request.Id, "Ana"), (offer.RequestId, offer.PassengerFirstName));
        Assert.True(offer.DetourKm <= 2);

        // aceitar: a passageira recebe TripUpdated
        var accepted = HubEvents.Next<TripDto>(anaHub, RealtimeEvents.TripUpdated, t => t.Status == "Accepted");
        var trip = await Ok<TripDto>(driver.PostAsync($"/api/ride-requests/{request.Id}/accept", null));
        Assert.Equal(("Now", "Accepted", "Bruno"), (trip.Mode, trip.Status, trip.Driver.FirstName));
        Assert.Equal(trip.Id, (await accepted).Id);
        Assert.Equal(trip.Id, (await Ok<RideRequestDto>(ana.GetAsync($"/api/ride-requests/{request.Id}"))).TripId);

        // posição ao vivo
        var location = HubEvents.Next<DriverLocationEvent>(anaHub, RealtimeEvents.DriverLocation);
        await driverHub.InvokeAsync("UpdateLocation", trip.Id, -25.4500, -49.2850);
        Assert.Equal(-25.4500, (await location).Latitude);
        var seen = await Ok<TripDto>(ana.GetAsync($"/api/trips/{trip.Id}"));
        Assert.Equal(("pickup", request.Price), (seen.EtaTo, seen.MyPayment!.Amount));
        Assert.NotNull(seen.EtaMinutes);

        // compartilhar trajeto: link público sem login
        var share = await Ok<ShareDto>(ana.PostAsync($"/api/trips/{trip.Id}/share", null));
        var anonymous = api.CreateClient();
        var publicTrip = await Ok<PublicTripDto>(anonymous.GetAsync($"/api/public/trips/{share.Token}"));
        Assert.Equal(-25.4500, publicTrip.DriverLocation!.Latitude);

        // chegou → embarcou → chegada
        var arrivedMsg = HubEvents.Next<MessageDto>(anaHub, RealtimeEvents.MessageReceived, m => m.Kind == "System");
        foreach (var status in new[] { "DriverArrived", "InProgress", "Completed" })
            Assert.Equal(status, (await Ok<TripDto>(driver.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status }))).Status);
        Assert.Contains("chegou", (await arrivedMsg).Text);

        // você chegou: Pix + nota
        var payment = await Ok<MyPaymentDto>(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/complete", new { paymentMethod = "Pix", rating = 5 }));
        Assert.Equal(("Pix", "PendingPayment", 5), (payment.Method, payment.Status, payment.Rating));
        Assert.Equal("already_paid", await Error(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/complete", new { paymentMethod = "Card", rating = 4 })));

        // terminou: link expira e vai para o histórico das duas pessoas
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/public/trips/{share.Token}")).StatusCode);
        var anaTrips = await Ok<MyTripsDto>(ana.GetAsync("/api/me/trips"));
        Assert.Contains(anaTrips.History, i => i.TripId == trip.Id && i.Mode == "Now" && i.Kind == "Passenger");
        var brunoTrips = await Ok<MyTripsDto>(driver.GetAsync("/api/me/trips"));
        var asDriver = Assert.Single(brunoTrips.History, i => i.TripId == trip.Id);
        Assert.Equal(offer.DriverAmount, asDriver.Amount); // motorista recebe o valor sem a taxa
        Assert.True(asDriver.Amount < request.Price);
    }

    [Fact]
    public async Task Motorista_indo_para_outro_lado_nao_recebe()
    {
        var (driver, _) = await api.NewDriverAsync();
        // Santa Felicidade → Pinhais: caminho que não passa perto
        await driver.PostAsJsonAsync("/api/driver/online",
            new { latitude = -25.4030, longitude = -49.3300, destinationLatitude = -25.4440, destinationLongitude = -49.1920 });
        var (ana, _) = await api.NewUserAsync();
        var request = await Ok<RideRequestDto>(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico));

        var open = await Ok<List<RideRequestOfferDto>>(driver.GetAsync("/api/driver/ride-requests"));
        Assert.DoesNotContain(open, o => o.RequestId == request.Id);
        await ana.DeleteAsync($"/api/ride-requests/{request.Id}");
        await driver.PostAsync("/api/driver/offline", null);
    }

    [Fact]
    public async Task Dois_motoristas_aceitando_so_um_leva_e_o_outro_e_avisado()
    {
        var (d1, _) = await api.NewDriverAsync();
        var (d2, _) = await api.NewDriverAsync();
        await d1.PostAsJsonAsync("/api/driver/online", DriverBatelToPolitecnico);
        await d2.PostAsJsonAsync("/api/driver/online", DriverBatelToPolitecnico);
        var (ana, _) = await api.NewUserAsync();
        var request = await Ok<RideRequestDto>(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico));

        var results = await Task.WhenAll(
            d1.PostAsync($"/api/ride-requests/{request.Id}/accept", null),
            d2.PostAsync($"/api/ride-requests/{request.Id}/accept", null));
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        await CancelActiveTripAsync(ana, request.Id);
        await d1.PostAsync("/api/driver/offline", null);
        await d2.PostAsync("/api/driver/offline", null);
    }

    [Fact]
    public async Task Passageiro_cancela_e_motorista_recebe_RideRequestClosed()
    {
        var (driver, _) = await api.NewDriverAsync();
        await using var hub = await api.ConnectHubAsync(driver);
        await driver.PostAsJsonAsync("/api/driver/online", DriverBatelToPolitecnico);
        var (ana, _) = await api.NewUserAsync();

        var offered = HubEvents.Next<RideRequestOfferDto>(hub, RealtimeEvents.RideRequested);
        var request = await Ok<RideRequestDto>(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico));
        await offered;
        Assert.Equal("request_open", await Error(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico)));

        var closed = HubEvents.Next<RideRequestClosedEvent>(hub, RealtimeEvents.RideRequestClosed, e => e.RequestId == request.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await ana.DeleteAsync($"/api/ride-requests/{request.Id}")).StatusCode);
        Assert.Equal("cancelled", (await closed).Reason);
        Assert.Equal("already_taken", await Error(driver.PostAsync($"/api/ride-requests/{request.Id}/accept", null)));
        await driver.PostAsync("/api/driver/offline", null);
    }

    [Fact]
    public async Task So_o_motorista_muda_status_e_nao_paga_antes_de_terminar()
    {
        var (driver, _) = await api.NewDriverAsync();
        await driver.PostAsJsonAsync("/api/driver/online", DriverBatelToPolitecnico);
        var (ana, _) = await api.NewUserAsync();
        var request = await Ok<RideRequestDto>(ana.PostAsJsonAsync("/api/ride-requests", AguaVerdeToPolitecnico));
        var trip = await Ok<TripDto>(driver.PostAsync($"/api/ride-requests/{request.Id}/accept", null));

        Assert.Equal("invalid_transition", await Error(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status = "DriverArrived" })));
        Assert.Equal("invalid_transition", await Error(driver.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status = "Completed" })));
        Assert.Equal("trip_not_finished", await Error(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/complete", new { paymentMethod = "Pix", rating = 5 })));
        Assert.Equal("invalid_payment_method", await Error(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/complete", new { paymentMethod = "Boleto" })));

        // passageira pode cancelar antes de embarcar
        Assert.Equal("Cancelled", (await Ok<TripDto>(ana.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status = "Cancelled" }))).Status);
        var (outsider, _) = await api.NewUserAsync();
        Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync($"/api/trips/{trip.Id}")).StatusCode);
    }

    [Fact]
    public async Task Chat_da_carona_marcada_e_sugestao_de_horario_ao_vivo()
    {
        var (driver, _) = await api.NewDriverAsync(fullName: "Bruno Lima");
        var ride = await RideTests.PublishAsync(driver, RideTests.Tomorrow(time: "08:00"));
        var (ana, _) = await api.NewUserAsync(fullName: "Ana Ribeiro");
        var (outsider, _) = await api.NewUserAsync();
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);
        await using var driverHub = await api.ConnectHubAsync(driver);
        await using var anaHub = await api.ConnectHubAsync(ana);
        await using var outsiderHub = await api.ConnectHubAsync(outsider);

        // mensagem pelo hub chega para o motorista; "digitando" também
        var typing = HubEvents.Next<TypingEvent>(driverHub, RealtimeEvents.Typing);
        await anaHub.InvokeAsync("Typing", ride.Id);
        Assert.Equal("Ana", (await typing).FirstName);
        var received = HubEvents.Next<MessageDto>(driverHub, RealtimeEvents.MessageReceived, m => m.Kind == "Text");
        var sent = await anaHub.InvokeAsync<MessageDto>("SendMessage", ride.Id, "Estou chegando!");
        Assert.Equal((sent.Id, "Ana"), ((await received).Id, (await received).AuthorFirstName));

        // quem não está na carona não manda nem lê
        await Assert.ThrowsAsync<HubException>(() => outsiderHub.InvokeAsync<MessageDto>("SendMessage", ride.Id, "oi"));
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.GetAsync($"/api/conversations/{ride.Id}/messages")).StatusCode);

        // sugestão: motorista recebe TimeSuggested e vira aviso no chat; aceitar avisa a passageira
        var suggested = HubEvents.Next<TimeSuggestionDto>(driverHub, RealtimeEvents.TimeSuggested);
        var s = await Ok<TimeSuggestionDto>(ana.PostAsJsonAsync($"/api/rides/{ride.Id}/time-suggestions", new { minutes = 10 }));
        Assert.Equal(s.Id, (await suggested).Id);
        var updated = HubEvents.Next<RideUpdatedEvent>(anaHub, RealtimeEvents.RideUpdated);
        await driver.PostAsync($"/api/rides/{ride.Id}/time-suggestions/{s.Id}/accept", null);
        Assert.Equal("08:10", (await updated).DepartureTime);

        var messages = await Ok<List<MessageDto>>(ana.GetAsync($"/api/conversations/{ride.Id}/messages"));
        Assert.Equal(new[] { "Text", "System", "System" }, messages.Select(m => m.Kind));
        Assert.Contains("10 min depois", messages[1].Text);
        Assert.Contains("08:10", messages[2].Text);
    }

    [Fact]
    public async Task Motorista_comeca_a_carona_marcada_e_vira_corrida()
    {
        var (driver, _) = await api.NewDriverAsync();
        var ride = await RideTests.PublishAsync(driver, RideTests.InMinutes(20));
        Assert.Equal("no_passengers", await Error(driver.PostAsync($"/api/rides/{ride.Id}/start", null)));

        var (ana, _) = await api.NewUserAsync();
        await ana.PostAsync($"/api/rides/{ride.Id}/reservations", null);
        var trip = await Ok<TripDto>(driver.PostAsync($"/api/rides/{ride.Id}/start", null));
        Assert.Equal(("Scheduled", ride.Id, ride.Id), (trip.Mode, trip.RideId, trip.ConversationId));
        Assert.Single(trip.Passengers);
        Assert.Equal("already_started", await Error(driver.PostAsync($"/api/rides/{ride.Id}/start", null)));

        var item = Assert.Single((await Ok<MyTripsDto>(ana.GetAsync("/api/me/trips"))).Upcoming, i => i.RideId == ride.Id);
        Assert.Equal(trip.Id, item.TripId);
        Assert.False(item.CanCancel); // já começou

        foreach (var status in new[] { "DriverArrived", "InProgress", "Completed" })
            await driver.PostAsJsonAsync($"/api/trips/{trip.Id}/status", new { status });
        Assert.Contains((await Ok<MyTripsDto>(ana.GetAsync("/api/me/trips"))).History, i => i.RideId == ride.Id);
    }

    private async Task CancelActiveTripAsync(HttpClient passenger, Guid requestId)
    {
        var tripId = (await Ok<RideRequestDto>(passenger.GetAsync($"/api/ride-requests/{requestId}"))).TripId!.Value;
        await passenger.PostAsJsonAsync($"/api/trips/{tripId}/status", new { status = "Cancelled" });
    }
}
