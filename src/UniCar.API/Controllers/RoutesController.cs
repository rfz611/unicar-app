using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UniCar.API.DTOs;
using UniCar.API.Services;
using UniCar.API.Services.Maps;
namespace UniCar.API.Controllers;
[ApiController,Route("api/routes")]
public class RoutesController(RouteQuoteService routes,PricingService pricing,IOptions<MapsOptions> maps):ControllerBase
{
 [Authorize,HttpPost("quotes")]public async Task<IActionResult> Create(RouteRequest request,CancellationToken ct)=>Ok(await routes.CreateAsync(TravelRules.Actor(User),request,ct));
 [Authorize,HttpPost("quotes/{id:guid}/suggestion")]public IActionResult Suggest(Guid id,SuggestionRequest request){var q=routes.GetValid(id,TravelRules.Actor(User));return Ok(pricing.Suggest(q.DistanceMeters,q.VehicleType,request.OriginalSeats,request.PricingMode));}
 [HttpGet("map-config")]public IActionResult Config()=>Ok(new{browserApiKey=maps.Value.BrowserApiKey,mapId=maps.Value.MapId,configured=!string.IsNullOrWhiteSpace(maps.Value.BrowserApiKey)});
}
