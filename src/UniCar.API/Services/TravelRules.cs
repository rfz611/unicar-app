using System.Security.Claims;
using UniCar.API.DTOs;
using UniCar.API.Models;
namespace UniCar.API.Services;
public sealed class TravelException(int status, string message) : Exception(message) { public int Status { get; } = status; }
public static class TravelRules
{
 public static int Actor(ClaimsPrincipal user) => int.TryParse((user.FindFirst("userId") ?? user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub"))?.Value, out var id) && id > 0 ? id : throw new TravelException(401,"Sua sessão expirou. Entre novamente.");
 public static void Amount(decimal amount) { if(amount<0 || amount>99999999.99m || decimal.Round(amount,2)!=amount) throw new TravelException(400,"Informe um valor em reais com até duas casas decimais."); }
 public static void Capacity(string vehicle,int seats) { if(vehicle is not ("CAR" or "MOTO") || seats<1 || seats>(vehicle=="MOTO"?1:4))throw new TravelException(400,"Verifique veículo e vagas: moto 1; carro até 4."); }
 public static RideSummaryDto Summary(Ride r) => new(r.Id,r.Origin,r.Destination,r.DepartureTime,r.VehicleType,r.Driver?.Name ?? "Motorista",r.PricingMode,r.ReferenceAmount,r.OriginalSeats);
 public static DateTimeOffset Utc(DateTime d)=>new(DateTime.SpecifyKind(d,DateTimeKind.Utc));
 public static void Point(MapPoint? p) { if(p is null || !double.IsFinite(p.Latitude)|| !double.IsFinite(p.Longitude)||p.Latitude is < -90 or >90||p.Longitude is < -180 or >180|| string.IsNullOrWhiteSpace(p.Label)||p.Label.Length>150||p.PlaceId?.Length>255)throw new TravelException(400,"Selecione e confirme um ponto válido no mapa."); }
}
