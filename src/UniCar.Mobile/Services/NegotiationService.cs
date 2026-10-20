using UniCar.Mobile.Models;
namespace UniCar.Mobile.Services;
public sealed class NegotiationService(TravelApiClient api)
{
 public Task<RideResult<NegotiationDto>> GetAsync(int id)=>api.SendAsync<NegotiationDto>(HttpMethod.Get,$"api/negotiations/{id}");
 public Task<RideResult<NegotiationDto>> SubmitAsync(int rideId,SubmitProposalRequest request)=>api.SendAsync<NegotiationDto>(HttpMethod.Post,$"api/rides/{rideId}/negotiations",request);
 public Task<RideResult<NegotiationDto>> CounterAsync(int id,SubmitProposalRequest request)=>api.SendAsync<NegotiationDto>(HttpMethod.Post,$"api/negotiations/{id}/counter",request);
 public Task<RideResult<System.Text.Json.JsonElement>> AcceptAsync(int id,int version)=>api.SendAsync<System.Text.Json.JsonElement>(HttpMethod.Post,$"api/negotiations/{id}/accept",new VersionRequest(version));
 public Task<RideResult<NegotiationDto>> DeclineAsync(int id,int version)=>api.SendAsync<NegotiationDto>(HttpMethod.Post,$"api/negotiations/{id}/decline",new VersionRequest(version));
 public Task<RideResult<NegotiationDto>> WithdrawAsync(int id,int version)=>api.SendAsync<NegotiationDto>(HttpMethod.Post,$"api/negotiations/{id}/withdraw",new VersionRequest(version));
}
