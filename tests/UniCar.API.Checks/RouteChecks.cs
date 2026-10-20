using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using UniCar.API.DTOs;
using UniCar.API.Services;
using UniCar.API.Services.Maps;
static class RouteChecks
{
 public static async Task Run()
 {
  var handler=new Reply();var provider=new GoogleRouteProvider(new HttpClient(handler),Options.Create(new MapsOptions{RoutesApiKey="fixture"}));
  var route=new RouteRequest(new(-25.4,-49.3,"Saída"),new(-25.5,-49.4,"Destino"),"MOTO");
  var result=await provider.ComputeAsync(route,default);
  if(result.DistanceMeters!=12000||result.DurationSeconds!=900||!handler.Body!.Contains("TWO_WHEELER"))throw new Exception("Route mapping failed");
  handler.Text="{\"routes\":[]}";
  try{await provider.ComputeAsync(route,default);throw new Exception("Empty route accepted");}catch(TravelException e)when(e.Status==422){}
  var missing=new GoogleRouteProvider(new HttpClient(handler),Options.Create(new MapsOptions()));
  try{await missing.ComputeAsync(route,default);throw new Exception("Missing key accepted");}catch(TravelException e)when(e.Status==503){}
  Console.WriteLine("PASS: route mapping, motorcycle mode, no route, missing key");
 }
 sealed class Reply:HttpMessageHandler
 {
  public string Text="{\"routes\":[{\"distanceMeters\":12000,\"duration\":\"900s\",\"polyline\":{\"encodedPolyline\":\"fixture-polyline\"}}]}";public string? Body;
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Body=await request.Content!.ReadAsStringAsync(ct);return new(HttpStatusCode.OK){Content=new StringContent(Text,Encoding.UTF8,"application/json")};}
 }
}
