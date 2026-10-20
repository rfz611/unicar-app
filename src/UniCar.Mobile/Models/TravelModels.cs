using System.Globalization;
namespace UniCar.Mobile.Models;
public record PlaceSuggestion(string Id,string Title,string Address);
public record MapPoint(double Latitude,double Longitude,string Label,string? PlaceId=null);
public record RouteRequest(MapPoint Origin,MapPoint Destination,string VehicleType);
public record RouteQuoteDto(Guid Id,MapPoint Origin,MapPoint Destination,string VehicleType,int DistanceMeters,int DurationSeconds,string EncodedPolyline,DateTimeOffset ExpiresAt);
public record PricingSuggestionDto(decimal? Amount,string Currency,string? UnavailableReason,decimal? CostPerKm,int OriginalSeats,string ConfigurationVersion);
public record SubmitProposalRequest(int ExpectedVersion,decimal Amount);
public record VersionRequest(int ExpectedVersion);
public record ProposalDto(int Id,int AuthorId,string AuthorName,decimal Amount,DateTimeOffset CreatedAt)
{
 public string Description=>$"{AuthorName} propôs {TravelText.Money(Amount)} · {CreatedAt.ToLocalTime():dd/MM HH:mm}";
}
public record RideSummaryDto(int Id,string Origin,string Destination,DateTime DepartureTime,string VehicleType,string DriverName,string PricingMode,decimal? ReferenceAmount, int OriginalSeats = 0)
{
 public string Route=>$"{Origin} → {Destination}";
 public string DateLabel=>$"{DepartureTime:dd/MM/yyyy · HH:mm} · {(VehicleType=="MOTO"?"Moto":"Carro")}";
}
public record NegotiationDto(int Id,int RideId,int PassengerId,int DriverId,string CounterpartyName,string Status,int Version,decimal LatestAmount,int LatestAuthorId,bool CanRespond,bool CanWithdraw,RideSummaryDto RideSummary,IReadOnlyList<ProposalDto> Proposals,int? ReservationId)
{
 public string Title=>$"{CounterpartyName} · {TravelText.Money(LatestAmount)}";
 public string StatusLabel=>TravelText.Status(Status,CanRespond);
 public bool CanReopen=>Status is "Declined" or "Withdrawn";
}
public record PageResult<T>(IReadOnlyList<T> Items,int Page,bool HasMore);
public record PassengerAgreementDto(string Name,decimal? AgreedAmount,string Currency);
public record ActivityTripDto(RideSummaryDto RideSummary,string Role,DateTimeOffset? ReservedAt,decimal? AgreedAmount,IReadOnlyList<PassengerAgreementDto> Passengers)
{
 public string PersonLabel=>Role=="Driver"?"Você ofereceu esta carona":RideSummary.DriverName;
 public string PriceLabel=>Role=="Driver"?(Passengers.Count==0?"Nenhuma reserva confirmada":string.Join(Environment.NewLine,Passengers.Select(p=>$"{p.Name} · {TravelText.Money(p.AgreedAmount)}"))):TravelText.Money(AgreedAmount);
 public string StatusLabel=>RideSummary.DepartureTime>DateTime.Now?"Próxima":"Passada";
 public string Details=>string.Join(Environment.NewLine,new[]{PersonLabel,RideSummary.Route,RideSummary.DateLabel,StatusLabel,PriceLabel,ReservedAt is null?"":$"Reserva em {ReservedAt.Value.ToLocalTime():dd/MM/yyyy HH:mm}"});
}
public static class TravelText
{
 public static string Money(decimal? amount)=>amount is null?"Valor não informado":amount==0?"Gratuita":amount.Value.ToString("C2",CultureInfo.GetCultureInfo("pt-BR"));
 public static string Status(string status,bool respond=false)=>status switch {"Pending"=>respond?"Aguardando sua resposta":"Aguardando resposta","Accepted"=>"Reserva confirmada","Declined"=>"Recusada","Withdrawn"=>"Retirada","Expired"=>"Prazo encerrado",_=>"Sem vagas"};
}
