using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UniCar.API.Controllers;
using UniCar.API.Data;
using UniCar.API.DTOs;
using UniCar.API.Models;
using UniCar.API.Services;
using UniCar.API.Services.Maps;
static class PublishChecks
{
 public static async Task Run(string connection)
 {
  var cs=new MySqlConnector.MySqlConnectionStringBuilder(connection);if(!cs.Database.StartsWith("unicar_check_"))throw new Exception("Isolated database required");
  using var db=new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySql(connection,ServerVersion.AutoDetect(connection)).Options);
  var driver=new User{Name="Route fixture",Email=Guid.NewGuid()+"@positivo.edu.br",PasswordHash="fixture",Phone="41999999999",RaEnrollment=Guid.NewGuid().ToString("N")[..20],UserType="DRIVER"};db.Users.Add(driver);await db.SaveChangesAsync();
  var time=new Clock();var routes=new RouteQuoteService(new Provider(),time);var price=new PricingService(Options.Create(new PricingOptions{CarCostPerKm=1m,ConfigurationVersion="fixture"}));
  var view=new NegotiationViews(db);var negotiation=new NegotiationService(db,view);
  var controller=new RidesController(db,negotiation,routes,price){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim("userId",driver.Id.ToString())},"fixture"))}}};
  var q=await routes.CreateAsync(driver.Id,new(new(-25,-49,"Origem"),new(-25.1,-49.1,"UP"),"CAR"),default);
  try{routes.GetValid(q.Id,driver.Id+1);throw new Exception("Wrong owner accepted");}catch(TravelException e)when(e.Status==409){}
  try{routes.GetValid(q.Id,driver.Id,"MOTO");throw new Exception("Wrong vehicle accepted");}catch(TravelException e)when(e.Status==400){}
  var request=new CreateRideDto{Origin="Origem",Destination="UP",DepartureTime=DateTime.Today.AddDays(1).AddHours(12),AvailableSeats=2,VehicleType="CAR",PricingMode=PricingMode.Free,RouteQuoteId=q.Id};
  if(await controller.CreateRide(request) is not OkObjectResult)throw new Exception("Publish with server quote failed");
  var ride=await db.Rides.SingleAsync(r=>r.DriverId==driver.Id);
  if(ride.OriginalSeats!=2||ride.ReferenceAmount!=0||ride.OriginLatitude!=-25)throw new Exception("Route/price not persisted");
  request.PricingMode=PricingMode.Negotiable;request.ReferenceAmount=9.5m;
  if(await controller.UpdatePricing(ride.Id,request,default) is not OkObjectResult)throw new Exception("Update existing offer failed");
  var passenger=new User{Name="Passenger fixture",Email=Guid.NewGuid()+"@positivo.edu.br",PasswordHash="fixture",Phone="41999999999",RaEnrollment=Guid.NewGuid().ToString("N")[..20],UserType="PASSENGER"};db.Users.Add(passenger);await db.SaveChangesAsync();
  db.RideReservations.Add(new(){RideId=ride.Id,PassengerId=passenger.Id,AgreedAmount=8m,PricingMode=PricingMode.Negotiable});await db.SaveChangesAsync();
  request.Origin="Outro encontro";
  try{await controller.UpdatePricing(ride.Id,request,default);throw new Exception("Confirmed route changed");}catch(TravelException e)when(e.Status==409){}
  db.ChangeTracker.Clear();request.Origin="Origem";request.ReferenceAmount=15m;
  await controller.UpdateReference(ride.Id,new ReferencePriceRequest(15m),default);
  if((await db.RideReservations.SingleAsync(r=>r.RideId==ride.Id)).AgreedAmount!=8m)throw new Exception("Agreed price changed with reference");
  var legacy=new Ride{DriverId=driver.Id,Origin="Origem",Destination="UP",DepartureTime=request.DepartureTime,AvailableSeats=1,OriginalSeats=2,VehicleType="CAR"};db.Rides.Add(legacy);await db.SaveChangesAsync();db.RideReservations.Add(new(){RideId=legacy.Id,PassengerId=passenger.Id});await db.SaveChangesAsync();
  request.PricingMode=PricingMode.Free;request.ReferenceAmount=0m;
  await controller.UpdatePricing(legacy.Id,request,default);
  if(legacy.AvailableSeats!=1||(await db.RideReservations.SingleAsync(b=>b.RideId==legacy.Id)).AgreedAmount is not null)throw new Exception("Legacy upgrade damaged prior reservations");
  time.Now=time.Now.AddMinutes(11);
  try{routes.GetValid(q.Id,driver.Id);throw new Exception("Expired quote accepted");}catch(TravelException e)when(e.Status==409){}
  Console.WriteLine("PASS: server quote publication/update, owner/mode/expiry checks, confirmed route and price preservation");
 }
 sealed class Provider:IRouteProvider{public Task<ProviderRoute> ComputeAsync(RouteRequest r,CancellationToken ct)=>Task.FromResult(new ProviderRoute(12000,900,"fixture-only"));}
 sealed class Clock:TimeProvider{public DateTimeOffset Now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Now;}
}
