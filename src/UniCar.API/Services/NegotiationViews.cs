using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using UniCar.API.DTOs;
using UniCar.API.Models;
namespace UniCar.API.Services;
public sealed class NegotiationViews(AppDbContext db)
{
 public async Task<NegotiationDto> GetAsync(int id,int actor,CancellationToken ct=default)
 {
  var n=await db.FareNegotiations.AsNoTracking().Include(n=>n.Ride).ThenInclude(r=>r.Driver).Include(n=>n.Passenger).SingleOrDefaultAsync(n=>n.Id==id,ct);
  if(n is null || (actor!=n.PassengerId && actor!=n.Ride.DriverId))throw new TravelException(404,"Proposta não encontrada.");
  var status=n.Status;
  if(status==NegotiationStatus.Pending && n.Ride.DepartureTime<=DateTime.Now)status=NegotiationStatus.Expired;
  else if(status==NegotiationStatus.Pending && n.Ride.AvailableSeats==0)status=NegotiationStatus.Unavailable;
  var proposals=await db.FareProposals.AsNoTracking().Where(p=>p.NegotiationId==id).Include(p=>p.Author).OrderBy(p=>p.Id).ToListAsync(ct);
  var reservation=await db.RideReservations.AsNoTracking().Where(r=>r.NegotiationId==id).Select(r=>(int?)r.Id).SingleOrDefaultAsync(ct);
  return new(n.Id,n.RideId,n.PassengerId,n.Ride.DriverId,actor==n.PassengerId?n.Ride.Driver!.Name:n.Passenger.Name,status,n.Version,n.LatestAmount,n.LatestAuthorId,status==NegotiationStatus.Pending&&n.LatestAuthorId!=actor,status==NegotiationStatus.Pending&&n.LatestAuthorId==actor,TravelRules.Summary(n.Ride),proposals.Select(p=>new ProposalDto(p.Id,p.AuthorId,p.Author.Name,p.Amount,TravelRules.Utc(p.CreatedAt))).ToList(),reservation);
 }
}
