using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCar.API.DTOs;
using UniCar.API.Services;
namespace UniCar.API.Controllers;
[ApiController,Authorize,Route("api/negotiations")]
public class NegotiationsController(NegotiationService negotiations,NegotiationViews views):ControllerBase
{
 [HttpPost("~/api/rides/{rideId:int}/negotiations")] public async Task<IActionResult> Submit(int rideId,SubmitProposalRequest request,CancellationToken ct)=>Ok(await negotiations.SubmitAsync(TravelRules.Actor(User),rideId,request,ct));
 [HttpGet("{id:int}")] public async Task<IActionResult> Get(int id,CancellationToken ct)=>Ok(await views.GetAsync(id,TravelRules.Actor(User),ct));
 [HttpPost("{id:int}/counter")] public async Task<IActionResult> Counter(int id,SubmitProposalRequest request,CancellationToken ct)=>Ok(await negotiations.RespondAsync(TravelRules.Actor(User),id,request.ExpectedVersion,"counter",request.Amount,ct));
 [HttpPost("{id:int}/accept")] public async Task<IActionResult> Accept(int id,VersionRequest request,CancellationToken ct)=>Ok(await negotiations.AcceptAsync(TravelRules.Actor(User),id,request.ExpectedVersion,ct));
 [HttpPost("{id:int}/decline")] public async Task<IActionResult> Decline(int id,VersionRequest request,CancellationToken ct)=>Ok(await negotiations.RespondAsync(TravelRules.Actor(User),id,request.ExpectedVersion,"decline",ct:ct));
 [HttpPost("{id:int}/withdraw")] public async Task<IActionResult> Withdraw(int id,VersionRequest request,CancellationToken ct)=>Ok(await negotiations.RespondAsync(TravelRules.Actor(User),id,request.ExpectedVersion,"withdraw",ct:ct));
}
