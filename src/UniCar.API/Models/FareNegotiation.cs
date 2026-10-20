using System.ComponentModel.DataAnnotations.Schema;
namespace UniCar.API.Models;
[Table("fare_negotiations")]
public class FareNegotiation
{
 public int Id { get; set; }
 public int RideId { get; set; }
 public int PassengerId { get; set; }
 public int LatestAuthorId { get; set; }
 public decimal LatestAmount { get; set; }
 public int Version { get; set; }
 public NegotiationStatus Status { get; set; } = NegotiationStatus.Pending;
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
 public Ride Ride { get; set; } = null!;
 public User Passenger { get; set; } = null!;
}
