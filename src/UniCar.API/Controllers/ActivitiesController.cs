using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCar.API.Services;
namespace UniCar.API.Controllers;
[ApiController,Authorize,Route("api/activities")]
public class ActivitiesController(ActivityService activities):ControllerBase
{
 [HttpGet("trips")] public async Task<IActionResult> Trips([FromQuery]int page=1,CancellationToken ct=default)=>Ok(await activities.GetTripsAsync(TravelRules.Actor(User),page,ct));
 [HttpGet("proposals")] public async Task<IActionResult> Proposals([FromQuery]int page=1,CancellationToken ct=default)=>Ok(await activities.GetProposalsAsync(TravelRules.Actor(User),page,ct));
}
