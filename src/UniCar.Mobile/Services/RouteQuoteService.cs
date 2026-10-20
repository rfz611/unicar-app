using UniCar.Mobile.Models;
namespace UniCar.Mobile.Services;
public sealed class RouteQuoteService(TravelApiClient api)
{
 public Task<RideResult<List<PlaceSuggestion>>> SearchPlacesAsync(string query,string session,CancellationToken ct)=>api.SendAsync<List<PlaceSuggestion>>(HttpMethod.Get,$"api/places?query={Uri.EscapeDataString(query)}&session={session}",ct:ct);
 public Task<RideResult<MapPoint>> ResolvePlaceAsync(string id,string session,CancellationToken ct)=>api.SendAsync<MapPoint>(HttpMethod.Get,$"api/places/resolve?id={Uri.EscapeDataString(id)}&session={session}",ct:ct);
 public Task<RideResult<RouteQuoteDto>> CreateAsync(RouteRequest request)=>api.SendAsync<RouteQuoteDto>(HttpMethod.Post,"api/routes/quotes",request);
 public Task<RideResult<PricingSuggestionDto>> SuggestAsync(Guid id,int seats,bool free)=>api.SendAsync<PricingSuggestionDto>(HttpMethod.Post,$"api/routes/quotes/{id}/suggestion",new{originalSeats=seats,pricingMode=free?"Free":"Negotiable"});
}
