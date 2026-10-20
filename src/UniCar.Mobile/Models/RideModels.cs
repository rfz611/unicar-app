namespace UniCar.Mobile.Models;

public sealed class Ride
{
    public string PricingMode { get; set; } = "LegacyUnknown";
    public decimal? ReferenceAmount { get; set; }
    public decimal? AgreedAmount { get; set; }
    public string PriceLabel => IsReserved?TravelText.Money(AgreedAmount):PricingMode=="Free"?"Gratuita":PricingMode=="Negotiable"?$"Referência: {TravelText.Money(ReferenceAmount)} por passageiro":"Valor não informado";
    public string ActionLabel => PricingMode=="Negotiable"?"Fazer proposta":"Reservar vaga";
    public string VehicleType { get; set; } = "CAR";
    public bool IsReserved { get; set; }
    public bool CanReserve => !IsReserved;
    public string VehicleLabel => VehicleType == "MOTO" ? "Moto" : "Carro";
    public string ReservationLabel => DepartureTime > DateTime.Now ? "Reserva confirmada" : "Viagem passada";
    public int Id { get; set; }
    public string Origin { get; set; } = "";
    public string Destination { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string DriverPhone { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
    public string DepartureLabel => DepartureTime.ToString("dd/MM · HH:mm");
    public string SeatsLabel => AvailableSeats == 1 ? "1 vaga disponível" : $"{AvailableSeats} vagas disponíveis";
}

public sealed record CreateRideRequest(string Origin, string Destination, DateTime DepartureTime, int AvailableSeats, string VehicleType = "CAR", string PricingMode = "Free", decimal? ReferenceAmount = null, Guid? RouteQuoteId = null);
public sealed record RideResult<T>(bool Success, T? Data, string? Error);
