using Microsoft.Extensions.Options;
using UniCar.API.DTOs;
using UniCar.API.Models;
namespace UniCar.API.Services;
public class PricingOptions
{
 public decimal? CarCostPerKm { get; set; }
 public decimal? MotoCostPerKm { get; set; }
 public string ConfigurationVersion { get; set; } = "unconfigured";
}
public sealed class PricingService(IOptions<PricingOptions> options)
{
 public PricingSuggestionDto Suggest(int distanceMeters,string vehicleType,int originalSeats,PricingMode mode)
 {
  TravelRules.Capacity(vehicleType,originalSeats);
  if(distanceMeters<=0)throw new TravelException(400,"Percurso inválido.");
  if(mode==PricingMode.Free)return new(0m,"BRL",null,null,originalSeats,options.Value.ConfigurationVersion);
  var cost=vehicleType=="MOTO"?options.Value.MotoCostPerKm:options.Value.CarCostPerKm;
  if(cost is null or <=0 || string.IsNullOrWhiteSpace(options.Value.ConfigurationVersion))return new(null,"BRL","Estimativa indisponível: custo por km não configurado.",null,originalSeats,options.Value.ConfigurationVersion);
  var amount=decimal.Round(distanceMeters/1000m*cost.Value/(originalSeats+1),2,MidpointRounding.AwayFromZero);
  TravelRules.Amount(amount);
  return new(amount,"BRL",null,cost,originalSeats,options.Value.ConfigurationVersion);
 }
}
