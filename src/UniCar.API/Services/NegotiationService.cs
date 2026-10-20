using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using UniCar.API.DTOs;
using UniCar.API.Models;
namespace UniCar.API.Services;

public sealed class NegotiationService(AppDbContext db,NegotiationViews views)
{
 private async Task<Ride> LockRide(int id,CancellationToken ct)
 {
  var rides=await db.Rides.FromSqlInterpolated($"SELECT * FROM rides WHERE id={id} FOR UPDATE").ToListAsync(ct);
  return rides.SingleOrDefault()??throw new TravelException(404,"Carona não encontrada.");
 }
 private static void Available(Ride ride)
 {
  if(ride.DepartureTime<=DateTime.Now)throw new TravelException(409,"Esta carona já saiu.");
  if(ride.AvailableSeats<=0)throw new TravelException(409,"Não há vagas disponíveis nesta carona.");
 }
 private static ReservationReceiptDto Receipt(RideReservation r)=>new(r.Id,r.RideId,r.AgreedAmount,r.Currency,r.PricingMode);
 private static void Version(FareNegotiation n,int expected){if(n.Version!=expected)throw new TravelException(409,"A proposta mudou. Atualize e confira o último valor.");}
 public async Task<NegotiationDto> SubmitAsync(int actor,int rideId,SubmitProposalRequest request,CancellationToken ct=default)
 {
  TravelRules.Amount(request.Amount);
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
  var ride=await LockRide(rideId,ct);Available(ride);
  if(ride.DriverId==actor)throw new TravelException(403,"Você não pode negociar uma vaga na própria carona.");
  if(ride.PricingMode!=PricingMode.Negotiable)throw new TravelException(400,"Esta carona não recebe propostas de contribuição.");
  if(await db.RideReservations.AnyAsync(b=>b.RideId==rideId&&b.PassengerId==actor,ct))throw new TravelException(409,"Você já tem uma reserva nesta carona.");
  var n=await db.FareNegotiations.SingleOrDefaultAsync(n=>n.RideId==rideId&&n.PassengerId==actor,ct);
  if(n is null){if(request.ExpectedVersion!=0)throw new TravelException(409,"Atualize as propostas.");n=new(){RideId=rideId,PassengerId=actor};db.FareNegotiations.Add(n);}
  else {Version(n,request.ExpectedVersion);if(n.Status is not (NegotiationStatus.Declined or NegotiationStatus.Withdrawn))throw new TravelException(409,"Esta conversa já tem uma proposta. Abra o detalhe para responder.");}
  n.Status=NegotiationStatus.Pending;n.LatestAmount=request.Amount;n.LatestAuthorId=actor;n.Version++;n.UpdatedAt=DateTime.UtcNow;
  db.FareProposals.Add(new(){Negotiation=n,AuthorId=actor,Amount=request.Amount});
  await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
  return await views.GetAsync(n.Id,actor,ct);
 }
 public async Task<NegotiationDto> RespondAsync(int actor,int id,int expected,string action,decimal? amount=null,CancellationToken ct=default)
 {
  if(amount.HasValue)TravelRules.Amount(amount.Value);
  var rideId=await db.FareNegotiations.AsNoTracking().Where(n=>n.Id==id&&(n.PassengerId==actor||n.Ride.DriverId==actor)).Select(n=>(int?)n.RideId).SingleOrDefaultAsync(ct)??throw new TravelException(404,"Proposta não encontrada.");
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
  var ride=await LockRide(rideId,ct);
  var n=(await db.FareNegotiations.FromSqlInterpolated($"SELECT * FROM fare_negotiations WHERE Id={id} FOR UPDATE").ToListAsync(ct)).Single();
  Version(n,expected);Available(ride);
  if(n.Status!=NegotiationStatus.Pending)throw new TravelException(409,"Esta proposta já foi encerrada.");
  if(action=="withdraw") {if(n.LatestAuthorId!=actor)throw new TravelException(403,"Só quem enviou a última proposta pode retirá-la.");n.Status=NegotiationStatus.Withdrawn;}
  else {
   if(n.LatestAuthorId==actor)throw new TravelException(403,"Aguarde a resposta da outra pessoa.");
   if(action=="counter"&&amount.HasValue){n.LatestAmount=amount.Value;n.LatestAuthorId=actor;db.FareProposals.Add(new(){NegotiationId=id,AuthorId=actor,Amount=amount.Value});}
   else if(action=="decline")n.Status=NegotiationStatus.Declined;
   else throw new TravelException(400,"Ação inválida.");
  }
  n.Version++;n.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
  return await views.GetAsync(id,actor,ct);
 }
 public async Task<ReservationReceiptDto> AcceptAsync(int actor,int id,int expected,CancellationToken ct=default)
 {
  var rideId=await db.FareNegotiations.AsNoTracking().Where(n=>n.Id==id&&(n.PassengerId==actor||n.Ride.DriverId==actor)).Select(n=>(int?)n.RideId).SingleOrDefaultAsync(ct)??throw new TravelException(404,"Proposta não encontrada.");
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
  var ride=await LockRide(rideId,ct);
  var n=(await db.FareNegotiations.FromSqlInterpolated($"SELECT * FROM fare_negotiations WHERE Id={id} FOR UPDATE").ToListAsync(ct)).Single();
  if(n.Status==NegotiationStatus.Accepted){var prior=await db.RideReservations.SingleAsync(b=>b.NegotiationId==id,ct);return Receipt(prior);}
  Version(n,expected);
  if(n.LatestAuthorId==actor)throw new TravelException(403,"Você não pode aceitar sua própria proposta.");
  if(n.Status!=NegotiationStatus.Pending)throw new TravelException(409,"Esta proposta já foi encerrada.");
  Available(ride);
  if(ride.PricingMode!=PricingMode.Negotiable)throw new TravelException(409,"As condições desta carona mudaram.");
  if(await db.RideReservations.AnyAsync(b=>b.RideId==rideId&&b.PassengerId==n.PassengerId,ct))throw new TravelException(409,"Já existe uma reserva nesta carona.");
  var reservation=new RideReservation{RideId=rideId,PassengerId=n.PassengerId,NegotiationId=id,AgreedAmount=n.LatestAmount,Currency="BRL",PricingMode=PricingMode.Negotiable};
  db.RideReservations.Add(reservation);ride.AvailableSeats--;n.Status=NegotiationStatus.Accepted;n.Version++;n.UpdatedAt=DateTime.UtcNow;
  await db.SaveChangesAsync(ct);
  if(ride.AvailableSeats==0)await MarkUnavailable(rideId,id,ct);
  await tx.CommitAsync(ct);return Receipt(reservation);
 }
 private Task<int> MarkUnavailable(int rideId,int acceptedId,CancellationToken ct)=>db.FareNegotiations.Where(n=>n.RideId==rideId&&n.Id!=acceptedId&&n.Status==NegotiationStatus.Pending).ExecuteUpdateAsync(s=>s.SetProperty(n=>n.Status,NegotiationStatus.Unavailable).SetProperty(n=>n.Version,n=>n.Version+1).SetProperty(n=>n.UpdatedAt,DateTime.UtcNow),ct);
 public async Task<ReservationReceiptDto> ReserveFreeAsync(int actor,int rideId,CancellationToken ct=default)
 {
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
  var ride=await LockRide(rideId,ct);
  if(ride.DriverId==actor)throw new TravelException(400,"Você é o motorista desta carona.");
  var existing=await db.RideReservations.SingleOrDefaultAsync(b=>b.RideId==rideId&&b.PassengerId==actor,ct);
  if(existing is not null)return Receipt(existing);
  if(ride.PricingMode!=PricingMode.Free)throw new TravelException(400,ride.PricingMode==PricingMode.LegacyUnknown?"O motorista ainda precisa informar o valor e atualizar o percurso desta carona.":"Envie uma proposta e confirme o valor antes de reservar.");
  Available(ride);
  var reservation=new RideReservation{RideId=rideId,PassengerId=actor,AgreedAmount=0m,PricingMode=PricingMode.Free};
  db.RideReservations.Add(reservation);ride.AvailableSeats--;await db.SaveChangesAsync(ct);
  if(ride.AvailableSeats==0)await MarkUnavailable(rideId,0,ct);
  await tx.CommitAsync(ct);return Receipt(reservation);
 }
}
