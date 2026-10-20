using System.Net;
using Microsoft.Extensions.Options;
using UniCar.API.Services.Maps;
using UniCar.API.Services;
public static class PlacesChecks
{
 public static async Task Run(){
  var handler=new Handler();var service=new GooglePlacesService(new HttpClient(handler),Options.Create(new MapsOptions{RoutesApiKey="fixture"}));
  var rows=await service.SearchAsync("Campus","session",default);
  if(rows.Count!=1||rows[0].Id!="fixture"||!handler.Body.Contains("\"br\""))throw new Exception("Brazil restriction or prediction parsing failed");
  var point=await service.ResolveAsync("fixture","session",default);
  if(point.Latitude!=-25||point.Label!="Campus")throw new Exception("Place selection failed");
  handler.Country="US";
  try{await service.ResolveAsync("fixture","session",default);throw new Exception("Foreign place accepted");}catch(TravelException e)when(e.Status==422){}
  Console.WriteLine("PASS: inline Places predictions, Brazil restriction, coordinates and foreign-place rejection");
 }
 private sealed class Handler:HttpMessageHandler{
  public string Body="",Country="BR";
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct){
   if(r.Method==HttpMethod.Post){Body=await r.Content!.ReadAsStringAsync(ct);return Json("{\"suggestions\":[{\"placePrediction\":{\"placeId\":\"fixture\",\"structuredFormat\":{\"mainText\":{\"text\":\"Campus\"},\"secondaryText\":{\"text\":\"Curitiba, Brasil\"}}}}]}");}
   return Json("{\"displayName\":{\"text\":\"Campus\"},\"formattedAddress\":\"Curitiba\",\"location\":{\"latitude\":-25,\"longitude\":-49},\"addressComponents\":[{\"shortText\":\""+Country+"\",\"types\":[\"country\"]}]}");
  }
  static HttpResponseMessage Json(string body)=>new(HttpStatusCode.OK){Content=new StringContent(body)};
 }
}
