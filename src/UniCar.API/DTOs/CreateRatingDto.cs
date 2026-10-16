namespace UniCar.API.DTOs;

public class CreateRatingDto
{
    public int Stars { get; set; } // 1 a 5
    public string Comment { get; set; } = string.Empty;
}