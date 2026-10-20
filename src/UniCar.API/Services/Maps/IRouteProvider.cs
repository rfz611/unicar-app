using UniCar.API.DTOs;
namespace UniCar.API.Services.Maps;
public record ProviderRoute(int DistanceMeters,int DurationSeconds,string EncodedPolyline);
public interface IRouteProvider { Task<ProviderRoute> ComputeAsync(RouteRequest request,CancellationToken ct); }
public class MapsOptions
{
 public string? RoutesApiKey { get; set; }
 public string? BrowserApiKey { get; set; }
 public string MapId { get; set; } = "DEMO_MAP_ID";
}
