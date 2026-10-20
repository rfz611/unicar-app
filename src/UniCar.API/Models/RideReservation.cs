using System.ComponentModel.DataAnnotations.Schema;

namespace UniCar.API.Models;

[Table("ride_reservations")]
public class RideReservation
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public int PassengerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(10,2)")] public decimal? AgreedAmount { get; set; }
    public string Currency { get; set; } = "BRL";
    public PricingMode PricingMode { get; set; } = PricingMode.LegacyUnknown;
    public int? NegotiationId { get; set; }
    public Ride Ride { get; set; } = null!;
}
