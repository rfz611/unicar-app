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
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        
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
}

public class CreateRideDto
{
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
}