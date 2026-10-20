using System.Text.Json;
using UniCar.Mobile.Models;
namespace UniCar.Mobile.Services;
public record MapBridgeMessage(string Nonce,string Action,int RequestId,RouteRequest? Request);
public static class RouteSelectionGuard
{
 public static bool TryRead(string url,string nonce,out MapBridgeMessage? message)
 {
  message=null;
  if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||uri.Scheme!="unicar-map"||uri.Host!="selection"||!uri.Query.StartsWith("?data=")||uri.Query.Length>20000)return false;
  try{
   message=JsonSerializer.Deserialize<MapBridgeMessage>(Uri.UnescapeDataString(uri.Query[6..]),new JsonSerializerOptions(JsonSerializerDefaults.Web));
   if(message is null||message.Nonce!=nonce||message.RequestId<0||message.Action is not ("selection" or "invalidate" or "location"))return false;
   if(message.Action!="selection")return true;
   var r=message.Request;
   return r is not null&&r.VehicleType is ("CAR" or "MOTO")&&Valid(r.Origin)&&Valid(r.Destination);
  }catch(JsonException){return false;}
 }
 private static bool Valid(MapPoint? p)=>p is not null&&double.IsFinite(p.Latitude)&&double.IsFinite(p.Longitude)&&p.Latitude is >=-90 and <=90&&p.Longitude is >=-180 and <=180&&!string.IsNullOrWhiteSpace(p.Label)&&p.Label.Length<=150;
}
