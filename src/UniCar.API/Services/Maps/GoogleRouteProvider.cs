using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UniCar.API.DTOs;
namespace UniCar.API.Services.Maps;
public sealed class GoogleRouteProvider(HttpClient http,IOptions<MapsOptions> options):IRouteProvider
{
 public async Task<ProviderRoute> ComputeAsync(RouteRequest route,CancellationToken ct)
 {
  if(string.IsNullOrWhiteSpace(options.Value.RoutesApiKey))throw new TravelException(503,"Mapas não configurados. Configure o serviço de rotas para calcular o percurso.");
  try{
   using var request=new HttpRequestMessage(HttpMethod.Post,"https://routes.googleapis.com/directions/v2:computeRoutes");
   request.Headers.Add("X-Goog-Api-Key",options.Value.RoutesApiKey);
   request.Headers.Add("X-Goog-FieldMask","routes.distanceMeters,routes.duration,routes.polyline.encodedPolyline");
   request.Content=JsonContent.Create(new { origin=new{location=new{latLng=new{latitude=route.Origin.Latitude,longitude=route.Origin.Longitude}}},destination=new{location=new{latLng=new{latitude=route.Destination.Latitude,longitude=route.Destination.Longitude}}},travelMode=route.VehicleType=="MOTO"?"TWO_WHEELER":"DRIVE",languageCode="pt-BR",units="METRIC" });
   using var response=await http.SendAsync(request,ct);
   if(!response.IsSuccessStatusCode)throw new TravelException(503,"O serviço de mapas está indisponível ou este modo de viagem não está habilitado. Tente novamente.");
   using var doc=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
   if(!doc.RootElement.TryGetProperty("routes",out var routes)||routes.GetArrayLength()==0)throw new TravelException(422,"Não foi encontrado um percurso para este veículo entre os pontos escolhidos.");
   var r=routes[0];var distance=r.GetProperty("distanceMeters").GetInt32();var seconds=r.GetProperty("duration").GetString()?.TrimEnd('s');
   if(!double.TryParse(seconds,NumberStyles.Float,CultureInfo.InvariantCulture,out var duration)||!double.IsFinite(duration)||duration<=0||duration>int.MaxValue||distance<=0)throw new JsonException();
   var polyline=r.GetProperty("polyline").GetProperty("encodedPolyline").GetString();
   if(string.IsNullOrEmpty(polyline)||polyline.Length>1000000)throw new JsonException();
   return new(distance,(int)Math.Ceiling(duration),polyline);
  }catch(TaskCanceledException)when(!ct.IsCancellationRequested){throw new TravelException(504,"O cálculo do percurso demorou. Tente novamente.");}
  catch(HttpRequestException){throw new TravelException(503,"Não foi possível conectar ao serviço de mapas.");}
  catch(Exception e)when(e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException){throw new TravelException(503,"O serviço de mapas retornou um percurso inválido.");}
 }
}
