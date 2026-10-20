using System.ComponentModel.DataAnnotations.Schema;
namespace UniCar.API.Models;
[Table("fare_proposals")]
public class FareProposal
{
 public int Id { get; set; }
 public int NegotiationId { get; set; }
 public int AuthorId { get; set; }
 public decimal Amount { get; set; }
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public FareNegotiation Negotiation { get; set; } = null!;
 public User Author { get; set; } = null!;
}
