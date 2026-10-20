using UniCar.Mobile.Models;
namespace UniCar.Mobile.Services;
public sealed class ActivityService(TravelApiClient api)
{
 public Task<RideResult<PageResult<ActivityTripDto>>> GetTripsAsync(int page=1)=>api.SendAsync<PageResult<ActivityTripDto>>(HttpMethod.Get,$"api/activities/trips?page={page}");
 public Task<RideResult<PageResult<NegotiationDto>>> GetProposalsAsync(int page=1)=>api.SendAsync<PageResult<NegotiationDto>>(HttpMethod.Get,$"api/activities/proposals?page={page}");
}
