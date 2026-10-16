namespace UniCar.API.Models;

public class Rating
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public int UserId { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}