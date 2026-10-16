using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using UniCar.API.Models;

namespace UniCar.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RidesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RidesController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/rides
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRide([FromBody] CreateRideDto dto)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" 
                                                       || c.Type == ClaimTypes.NameIdentifier
                                                       || c.Type == "sub");
        
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int driverId))
            return Unauthorized(new { message = "Token inválido ou utilizador não identificado." });

        var ride = new Ride
        {
            DriverId = driverId,
            Origin = dto.Origin.Trim(),
            Destination = dto.Destination.Trim(),
            DepartureTime = dto.DepartureTime,
            AvailableSeats = dto.AvailableSeats
        };

        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Carona publicada com sucesso!", rideId = ride.Id });
    }

    // GET: api/rides?destination=Campus&date=2026-10-01
    [HttpGet]
    public async Task<IActionResult> GetRides([FromQuery] string? destination, [FromQuery] DateTime? date)
    {
        var query = _context.Rides.AsQueryable();

        if (!string.IsNullOrWhiteSpace(destination))
            query = query.Where(r => r.Destination.Contains(destination));

        if (date.HasValue)
            query = query.Where(r => r.DepartureTime.Date == date.Value.Date);

        var rides = await query
            .Where(r => r.AvailableSeats > 0)
            .Include(r => r.Driver)
            .Select(r => new
            {
                r.Id,
                r.Origin,
                r.Destination,
                r.DepartureTime,
                r.AvailableSeats,
                DriverName = r.Driver!.Name,
                DriverPhone = r.Driver.Phone
            })
            .ToListAsync();

        return Ok(rides);
    }

    // POST: api/rides/1/join
    [HttpPost("{id}/join")]
    [Authorize]
    public async Task<IActionResult> JoinRide(int id)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" 
                                                       || c.Type == ClaimTypes.NameIdentifier
                                                       || c.Type == "sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int passengerId))
            return Unauthorized(new { message = "Token inválido ou utilizador não identificado." });

        var ride = await _context.Rides.FindAsync(id);
        if (ride == null)
            return NotFound(new { message = "Carona não encontrada." });

        if (ride.AvailableSeats <= 0)
            return BadRequest(new { message = "Não há vagas disponíveis para esta carona." });

        if (ride.DriverId == passengerId)
            return BadRequest(new { message = "Você é o motorista desta carona." });

        ride.AvailableSeats -= 1;
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            message = "Reserva realizada com sucesso!", 
            rideId = ride.Id, 
            remainingSeats = ride.AvailableSeats 
        });
    }

    // POST: api/rides/1/ratings (Item 2.5)
    [HttpPost("{id}/ratings")]
    [Authorize]
    public async Task<IActionResult> RateRide(int id, [FromBody] CreateRatingDto dto)
    {
        if (dto.Stars < 1 || dto.Stars > 5)
            return BadRequest(new { message = "A nota deve ser entre 1 e 5 estrelas." });

        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" 
                                                       || c.Type == ClaimTypes.NameIdentifier
                                                       || c.Type == "sub");

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "Token inválido ou utilizador não identificado." });

        var ride = await _context.Rides.FindAsync(id);
        if (ride == null)
            return NotFound(new { message = "Carona não encontrada." });

        var rating = new Rating
        {
            RideId = id,
            UserId = userId,
            Stars = dto.Stars,
            Comment = dto.Comment.Trim()
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Avaliação registrada com sucesso!", ratingId = rating.Id });
    }
}

public class CreateRideDto
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
}

public class CreateRatingDto
{
    public int Stars { get; set; }
    public string Comment { get; set; } = string.Empty;
}