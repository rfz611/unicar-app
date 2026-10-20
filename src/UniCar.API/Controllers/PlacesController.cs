using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniCar.API.Services.Maps;
namespace UniCar.API.Controllers;
[Authorize,ApiController,Route("api/places")]
public class PlacesController(GooglePlacesService places):ControllerBase
{
 [HttpGet]public async Task<IActionResult> Search([FromQuery]string? query,[FromQuery]string session,CancellationToken ct)=>Ok(await places.SearchAsync(query,session,ct));
 [HttpGet("resolve")]public async Task<IActionResult> Resolve([FromQuery]string id,[FromQuery]string session,CancellationToken ct)=>Ok(await places.ResolveAsync(id,session,ct));
}
