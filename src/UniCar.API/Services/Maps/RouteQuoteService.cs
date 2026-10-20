using Microsoft.Extensions.Caching.Memory;
using UniCar.API.DTOs;
namespace UniCar.API.Services.Maps;
public sealed class RouteQuoteService(IRouteProvider provider,TimeProvider time)
{
 private static readonly MemoryCache Cache=new(new MemoryCacheOptions{SizeLimit=2000});
 private static readonly object Gate=new();
 private record Stored(int UserId,RouteQuoteDto Quote);
 public async Task<RouteQuoteDto> CreateAsync(int userId,RouteRequest request,CancellationToken ct)
 {
  TravelRules.Point(request.Origin);TravelRules.Point(request.Destination);TravelRules.Capacity(request.VehicleType,1);
  if(request.Origin.Latitude==request.Destination.Latitude&&request.Origin.Longitude==request.Destination.Longitude)throw new TravelException(400,"Escolha pontos diferentes de saída e destino.");
  lock(Gate){var key=$"limit:{userId}";var count=Cache.Get<int?>(key)??0;if(count>=10)throw new TravelException(429,"Aguarde um minuto antes de calcular outro percurso.");Cache.Set(key,count+1,new MemoryCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(1),Size=1});}
  var result=await provider.ComputeAsync(request,ct);
  var quote=new RouteQuoteDto(Guid.NewGuid(),request.Origin,request.Destination,request.VehicleType,result.DistanceMeters,result.DurationSeconds,result.EncodedPolyline,time.GetUtcNow().AddMinutes(10));
  Cache.Set(quote.Id,new Stored(userId,quote),new MemoryCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(10),Size=1});
  return quote;
 }
 public RouteQuoteDto GetValid(Guid id,int userId,string? vehicleType=null)
 {
  if(!Cache.TryGetValue(id,out Stored? stored)||stored is null||stored.UserId!=userId||stored.Quote.ExpiresAt<=time.GetUtcNow())throw new TravelException(409,"O percurso expirou. Calcule novamente no mapa.");
  if(vehicleType is not null&&stored.Quote.VehicleType!=vehicleType)throw new TravelException(400,"O percurso não corresponde ao veículo selecionado.");
  return stored.Quote;
 }
}
