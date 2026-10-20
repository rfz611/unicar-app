using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UniCar.API.DTOs;
namespace UniCar.API.Services.Maps;
public record PlaceSuggestion(string Id,string Title,string Address);
public sealed class GooglePlacesService(HttpClient http,IOptions<MapsOptions> options)
{
 private HttpRequestMessage Request(HttpMethod method,string path){
  var key=options.Value.RoutesApiKey;
  if(string.IsNullOrWhiteSpace(key))throw new TravelException(503,"Busca de endereços não configurada.");
  var request=new HttpRequestMessage(method,"https://places.googleapis.com/v1/"+path);request.Headers.Add("X-Goog-Api-Key",key);return request;
 }
 private static void Session(string session){if(string.IsNullOrWhiteSpace(session)||session.Length>64||session.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='-'))throw new TravelException(400,"Pesquisa inválida.");}
 private async Task<JsonDocument> Send(HttpRequestMessage request,CancellationToken ct){
  try{using var response=await http.SendAsync(request,ct);if(!response.IsSuccessStatusCode)throw new TravelException(503,"Não foi possível buscar endereços. Tente novamente.");return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));}
  catch(HttpRequestException){throw new TravelException(503,"Não foi possível conectar à busca de endereços.");}
  catch(TaskCanceledException)when(!ct.IsCancellationRequested){throw new TravelException(504,"A busca demorou. Tente novamente.");}
 }
 public async Task<List<PlaceSuggestion>> SearchAsync(string? query,string session,CancellationToken ct){
  Session(session);query=query?.Trim();if(string.IsNullOrEmpty(query)||query.Length<3)return [];if(query.Length>150)throw new TravelException(400,"Endereço muito longo.");
  using var request=Request(HttpMethod.Post,"places:autocomplete");request.Content=JsonContent.Create(new{input=query,includedRegionCodes=new[]{"br"},languageCode="pt-BR",sessionToken=session});
  using var doc=await Send(request,ct);var result=new List<PlaceSuggestion>();
  if(doc.RootElement.TryGetProperty("suggestions",out var rows))foreach(var row in rows.EnumerateArray().Take(5)){
   if(!row.TryGetProperty("placePrediction",out var p))continue;
   var format=p.GetProperty("structuredFormat");var title=format.GetProperty("mainText").GetProperty("text").GetString()??"Local";
   var address=format.TryGetProperty("secondaryText",out var a)?a.GetProperty("text").GetString()??"":"";
   result.Add(new(p.GetProperty("placeId").GetString()!,title,address));
  }return result;
 }
 public async Task<MapPoint> ResolveAsync(string id,string session,CancellationToken ct){
  Session(session);if(string.IsNullOrWhiteSpace(id)||id.Length>300)throw new TravelException(400,"Local inválido.");
  using var request=Request(HttpMethod.Get,"places/"+Uri.EscapeDataString(id)+"?languageCode=pt-BR&sessionToken="+Uri.EscapeDataString(session));
  request.Headers.Add("X-Goog-FieldMask","id,displayName,formattedAddress,location,addressComponents");
  using var doc=await Send(request,ct);var root=doc.RootElement;
  if(!root.TryGetProperty("addressComponents",out var components)||!components.EnumerateArray().Any(c=>c.GetProperty("types").EnumerateArray().Any(t=>t.GetString()=="country")&&c.GetProperty("shortText").GetString()=="BR"))throw new TravelException(422,"Selecione um local no Brasil.");
  var location=root.GetProperty("location");var label=root.GetProperty("displayName").GetProperty("text").GetString()??root.GetProperty("formattedAddress").GetString()!;
  return new(location.GetProperty("latitude").GetDouble(),location.GetProperty("longitude").GetDouble(),label[..Math.Min(label.Length,150)],id);
 }
}
