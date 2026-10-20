using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using UniCar.API.DTOs;
namespace UniCar.API.Services;
public sealed class ActivityService(AppDbContext db,NegotiationViews views)
{
 private static int Offset(int page) { if(page<1||page>10000)throw new TravelException(400,"Página inválida.");return (page-1)*20; }
 public async Task<PageResult<ActivityTripDto>> GetTripsAsync(int userId,int page,CancellationToken ct=default)
 {
  var rows=await db.Rides.AsNoTracking().Where(r=>r.DriverId==userId||db.RideReservations.Any(b=>b.RideId==r.Id&&b.PassengerId==userId)).Include(r=>r.Driver).OrderByDescending(r=>r.DepartureTime).ThenByDescending(r=>r.Id).Skip(Offset(page)).Take(21).ToListAsync(ct);
  var hasMore=rows.Count>20;rows=rows.Take(20).ToList();var ids=rows.Select(r=>r.Id).ToList();
  var bookings=await db.RideReservations.AsNoTracking().Where(b=>ids.Contains(b.RideId)).ToListAsync(ct);
  var passengers=bookings.Select(b=>b.PassengerId).Distinct().ToList();
  var names=await db.Users.AsNoTracking().Where(u=>passengers.Contains(u.Id)).ToDictionaryAsync(u=>u.Id,u=>u.Name,ct);
  var items=rows.Select(r=>{
   var mine=bookings.FirstOrDefault(b=>b.RideId==r.Id&&b.PassengerId==userId);
   var driver=r.DriverId==userId;
   return new ActivityTripDto(TravelRules.Summary(r),driver?"Driver":"Passenger",mine is null?null:TravelRules.Utc(mine.CreatedAt),driver?null:mine?.AgreedAmount,driver?bookings.Where(b=>b.RideId==r.Id).Select(b=>new PassengerAgreementDto(names.GetValueOrDefault(b.PassengerId,"Passageiro"),b.AgreedAmount,b.Currency)).ToList():[]);
  }).ToList();
  return new(items,page,hasMore);
 }
 public async Task<PageResult<NegotiationDto>> GetProposalsAsync(int userId,int page,CancellationToken ct=default)
 {
  var ids=await db.FareNegotiations.AsNoTracking().Where(n=>n.PassengerId==userId||n.Ride.DriverId==userId).OrderByDescending(n=>n.UpdatedAt).ThenByDescending(n=>n.Id).Skip(Offset(page)).Take(21).Select(n=>n.Id).ToListAsync(ct);
  var items=new List<NegotiationDto>();foreach(var id in ids.Take(20))items.Add(await views.GetAsync(id,userId,ct));
  return new(items,page,ids.Count>20);
 }
}
