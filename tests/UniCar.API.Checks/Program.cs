using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using Microsoft.Extensions.Options;
using UniCar.API.Models;
using UniCar.API.Services;
if(args.Contains("initdb")||args.Contains("publish"))
{
 var connection=Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? throw new Exception("Test connection required");
 var builder=new MySqlConnector.MySqlConnectionStringBuilder(connection);
 if(!builder.Database.StartsWith("unicar_check_"))throw new Exception("Use isolated test database");
 using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySql(connection,ServerVersion.AutoDetect(connection)).Options);
 db.Database.EnsureCreated();if(args.Contains("publish"))await PublishChecks.Run(connection);return;
}
var service=new PricingService(Options.Create(new PricingOptions { CarCostPerKm=1m, MotoCostPerKm=1.01m, ConfigurationVersion="fixture" }));
var count=0;
void Check(bool ok,string message){ if(!ok)throw new Exception(message);count++; }
Check(service.Suggest(12000,"CAR",3,PricingMode.Negotiable).Amount==3m,"Split original capacity");
Check(service.Suggest(1000,"MOTO",1,PricingMode.Negotiable).Amount==0.51m,"Round midpoint away from zero");
Check(service.Suggest(12000,"CAR",3,PricingMode.Free).Amount==0m,"Free is explicit zero");
Check(new PricingService(Options.Create(new PricingOptions())).Suggest(12000,"CAR",3,PricingMode.Negotiable).Amount is null,"Missing config is unknown");
foreach(var amount in new[]{-1m,1.234m,100000000m}) { try{TravelRules.Amount(amount);throw new Exception("Invalid amount accepted");}catch(TravelException){count++;} }
TravelRules.Amount(0m); count++;
Console.WriteLine($"PASS: {count} API unit checks");

await RouteChecks.Run();

await PlacesChecks.Run();
