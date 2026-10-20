using System.Net;
using System.Text;
using UniCar.Mobile.Services;
using UniCar.Mobile.Models;

var count = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
var handler = new ReplyHandler();
var service = new RideService(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
handler.Body = """[{"id":7,"origin":"Centro","destination":"UP","driverName":"Ana","driverPhone":"41999999999","departureTime":"2026-10-21T08:30:00","availableSeats":2}]""";
var list = await service.SearchAsync(" Campus & UP ");
Check(list.Success && list.Data?.Count == 1 && list.Data[0].DriverName == "Ana", "Deserialize API ride list");
Check(handler.Url == "http://localhost/api/rides?destination=Campus%20%26%20UP", "Escape destination as a single query value");
handler.Status = HttpStatusCode.Unauthorized;
var denied = await service.JoinAsync(7, "token");
Check(!denied.Success && denied.Error!.Contains("sessão"), "Explain expired session");
Check(handler.Method == HttpMethod.Post && handler.Authorization == "Bearer token", "Authenticate reservation");
handler.Status = HttpStatusCode.BadRequest; handler.Body = """{"message":"Não há vagas disponíveis para esta carona."}""";
var full = await service.JoinAsync(7, "token");
Check(!full.Success && full.Error!.Contains("vagas"), "Preserve API business error");
handler.Status = HttpStatusCode.OK; handler.Body = "not json";
Check(!(await service.SearchAsync("")).Success, "Handle malformed API JSON");
handler.Body = "null";
Check(!(await service.SearchAsync("")).Success, "Do not treat null response as empty results");
handler.ThrowNetwork = true;
Check(!(await service.SearchAsync("")).Success, "Handle offline server");
handler.ThrowNetwork = false; handler.Body = """{"message":"Carona publicada com sucesso!"}""";
var created = await service.CreateAsync(new CreateRideRequest("Centro", "UP", DateTime.Today.AddDays(1), 3), "token");
Check(created.Success && handler.Method == HttpMethod.Post && handler.Url!.EndsWith("/api/rides"), "Publish to API route");
Check(handler.RequestBody!.Contains("\"availableSeats\":3"), "Send seats in create request");
handler.Body = "[]";
await service.SearchAsync("UP", "MOTO", new DateTime(2026, 10, 22));
Check(handler.Url == "http://localhost/api/rides?destination=UP&vehicleType=MOTO&date=2026-10-22", "Filter transport and scheduled date");
await service.ReservationsAsync("token");
Check(handler.Url!.EndsWith("/api/rides/reservations") && handler.Authorization == "Bearer token", "Load current user's saved reservations");
Check(MoneyInput.TryParse("12,50",out var br)&&br==12.5m,"Parse pt-BR money");
Check(MoneyInput.TryParse("12.50",out var dot)&&dot==12.5m,"Parse dot decimal");
foreach(var text in new[]{"", "-1", "12,345", "1.234,56"})Check(!MoneyInput.TryParse(text,out _),"Reject ambiguous money");
Check(MoneyInput.TryParse("0",out var free)&&free==0,"Allow zero proposal");
Check(!RouteSelectionGuard.TryRead("https://evil.invalid/selection?data={}","nonce",out _),"Reject foreign map scheme");
var bridge="unicar-map://selection?data="+Uri.EscapeDataString("{\"nonce\":\"other\",\"action\":\"location\",\"requestId\":1}");
Check(!RouteSelectionGuard.TryRead(bridge,"nonce",out _),"Reject map nonce mismatch");
var reload=new AsyncReloadGate();
Check(reload.TryStart(),"Start page load");
Check(!reload.TryStart(),"Coalesce reappearing during pending load");
Check(reload.Complete(true),"Queue reload when page has reappeared");
Check(reload.TryStart()&&!reload.Complete(true),"Finish subsequent load without looping");
var priceDraft=new PriceDraftGuard();var version=priceDraft.Version;priceDraft.UserEdited();
Check(!priceDraft.CanApplySuggestion(version),"Delayed suggestion cannot replace manual price");
Check(!SessionResponseGuard.Matches("previous-account","new-account"),"Do not confirm a previous account's request");
Check(SessionResponseGuard.Matches("same-account","same-account"),"Confirm only the original session");
Console.WriteLine($"PASS: {count} service checks");

sealed class ReplyHandler : HttpMessageHandler
{
    public string Body = "[]";
    public HttpStatusCode Status = HttpStatusCode.OK;
    public bool ThrowNetwork;
    public string? Url, Authorization, RequestBody;
    public HttpMethod? Method;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (ThrowNetwork) throw new HttpRequestException("offline");
        Url = request.RequestUri!.AbsoluteUri; Method = request.Method;
        Authorization = request.Headers.Authorization?.ToString();
        RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
        return new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") };
    }
}
