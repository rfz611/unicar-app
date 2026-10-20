using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniCar.API.Data;
using UniCar.API.Models;
using UniCar.API.Services;
using UniCar.API.Services.Maps;
using UniCar.API.DTOs;

namespace UniCar.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RidesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly NegotiationService _negotiations;
    private readonly RouteQuoteService _routes;
    private readonly PricingService _pricing;

    public RidesController(AppDbContext context, NegotiationService negotiations,RouteQuoteService routes,PricingService pricing)
    {
        _context = context;
        _negotiations = negotiations;
        _routes=routes; _pricing=pricing;
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

        var vehicleType = dto.VehicleType.Trim().ToUpperInvariant();
        if (vehicleType is not ("CAR" or "MOTO") || dto.AvailableSeats < 1 || dto.AvailableSeats > (vehicleType == "MOTO" ? 1 : 4)
            || string.IsNullOrWhiteSpace(dto.Origin) || string.IsNullOrWhiteSpace(dto.Destination)
            || dto.Origin.Length > 150 || dto.Destination.Length > 150 || dto.DepartureTime <= DateTime.Now)
            return BadRequest(new { message = "Verifique trajeto, horário futuro e vagas (moto: 1; carro: até 4)." });
        var driver = await _context.Users.FindAsync(driverId);
        if (driver is null || driver.UserType is not ("DRIVER" or "BOTH"))
            return StatusCode(403, new { message = "É necessário um perfil de motorista para publicar." });

        var quote=_routes.GetValid(dto.RouteQuoteId,driverId,vehicleType);
        if(dto.PricingMode is not (PricingMode.Free or PricingMode.Negotiable))throw new TravelException(400,"Escolha carona gratuita ou contribuição negociável.");
        if(dto.PricingMode==PricingMode.Negotiable && dto.ReferenceAmount is null)throw new TravelException(400,"Informe a contribuição de referência.");
        if(dto.ReferenceAmount.HasValue)TravelRules.Amount(dto.ReferenceAmount.Value);
        var suggestion=_pricing.Suggest(quote.DistanceMeters,vehicleType,dto.AvailableSeats,dto.PricingMode);
        var ride = new Ride
        {
            DriverId = driverId,
            OriginalSeats=dto.AvailableSeats, PricingMode=dto.PricingMode,
            ReferenceAmount=dto.PricingMode==PricingMode.Free?0m:dto.ReferenceAmount,
            OriginLatitude=quote.Origin.Latitude, OriginLongitude=quote.Origin.Longitude,
            DestinationLatitude=quote.Destination.Latitude, DestinationLongitude=quote.Destination.Longitude,
            OriginPlaceId=quote.Origin.PlaceId, DestinationPlaceId=quote.Destination.PlaceId,
            PricingConfigVersion=suggestion.ConfigurationVersion,
            Origin = dto.Origin.Trim(),
            Destination = dto.Destination.Trim(),
            DepartureTime = dto.DepartureTime,
            VehicleType = vehicleType,
            AvailableSeats = dto.AvailableSeats
        };

        _context.Rides.Add(ride);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Carona publicada com sucesso!", rideId = ride.Id });
    }

    [HttpPatch("{id:int}/pricing-route")]
    [Authorize]
    public async Task<IActionResult> UpdatePricing(int id,CreateRideDto dto,CancellationToken ct)
    {
        var actor=TravelRules.Actor(User);
        await using var tx=await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
        var ride=(await _context.Rides.FromSqlInterpolated($"SELECT * FROM rides WHERE id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw new TravelException(404,"Carona não encontrada.");
        if(ride.DriverId!=actor)throw new TravelException(403,"Somente o motorista pode atualizar a carona.");
        if(ride.DepartureTime<=DateTime.Now)throw new TravelException(409,"Esta carona já saiu.");
        TravelRules.Capacity(dto.VehicleType,dto.AvailableSeats);
        if(dto.PricingMode is not (PricingMode.Free or PricingMode.Negotiable))throw new TravelException(400,"Escolha a condição de contribuição.");
        if(dto.PricingMode==PricingMode.Negotiable&&dto.ReferenceAmount is null)throw new TravelException(400,"Informe o valor de referência.");
        if(dto.ReferenceAmount.HasValue)TravelRules.Amount(dto.ReferenceAmount.Value);
        var quote=_routes.GetValid(dto.RouteQuoteId,actor,dto.VehicleType);
        if(string.IsNullOrWhiteSpace(dto.Origin)||string.IsNullOrWhiteSpace(dto.Destination)||dto.Origin.Length>150||dto.Destination.Length>150)throw new TravelException(400,"Confirme os pontos de encontro.");
        var protectedRide=await _context.RideReservations.AnyAsync(b=>b.RideId==id,ct)||await _context.FareNegotiations.AnyAsync(n=>n.RideId==id&&n.Status==NegotiationStatus.Pending,ct);
        var legacyUpgrade=ride.PricingMode==PricingMode.LegacyUnknown&&ride.OriginLatitude is null&&ride.DestinationLatitude is null;
        var changedBase=ride.VehicleType!=dto.VehicleType||ride.OriginalSeats!=dto.AvailableSeats||ride.DepartureTime!=dto.DepartureTime||ride.Origin!=dto.Origin.Trim()||ride.Destination!=dto.Destination.Trim();
        var changedRoute=ride.OriginLatitude!=quote.Origin.Latitude||ride.OriginLongitude!=quote.Origin.Longitude||ride.DestinationLatitude!=quote.Destination.Latitude||ride.DestinationLongitude!=quote.Destination.Longitude;
        if(protectedRide&&(changedBase||(!legacyUpgrade&&(changedRoute||ride.PricingMode!=dto.PricingMode))))throw new TravelException(409,"Há reservas ou propostas pendentes. O trajeto e as condições precisam ser preservados.");
        if(dto.DepartureTime<=DateTime.Now)throw new TravelException(400,"Escolha um horário futuro.");
        if(!protectedRide||legacyUpgrade){ride.Origin=dto.Origin.Trim();ride.Destination=dto.Destination.Trim();ride.VehicleType=dto.VehicleType;ride.PricingMode=dto.PricingMode;ride.DepartureTime=dto.DepartureTime;ride.OriginalSeats=dto.AvailableSeats;if(!protectedRide)ride.AvailableSeats=dto.AvailableSeats;ride.OriginLatitude=quote.Origin.Latitude;ride.OriginLongitude=quote.Origin.Longitude;ride.DestinationLatitude=quote.Destination.Latitude;ride.DestinationLongitude=quote.Destination.Longitude;ride.OriginPlaceId=quote.Origin.PlaceId;ride.DestinationPlaceId=quote.Destination.PlaceId;}
        ride.ReferenceAmount=dto.PricingMode==PricingMode.Free?0m:dto.ReferenceAmount;
        ride.PricingConfigVersion=_pricing.Suggest(quote.DistanceMeters,dto.VehicleType,dto.AvailableSeats,dto.PricingMode).ConfigurationVersion;
        await _context.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Ok(new{message="Carona atualizada.",rideId=id});
    }

    [HttpPatch("{id:int}/reference")]
    [Authorize]
    public async Task<IActionResult> UpdateReference(int id,ReferencePriceRequest dto,CancellationToken ct)
    {
        TravelRules.Amount(dto.ReferenceAmount);var actor=TravelRules.Actor(User);
        await using var tx=await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
        var ride=(await _context.Rides.FromSqlInterpolated($"SELECT * FROM rides WHERE id={id} FOR UPDATE").ToListAsync(ct)).SingleOrDefault()??throw new TravelException(404,"Carona não encontrada.");
        if(ride.DriverId!=actor)throw new TravelException(403,"Somente o motorista pode atualizar a referência.");
        if(ride.DepartureTime<=DateTime.Now||ride.PricingMode!=PricingMode.Negotiable)throw new TravelException(409,"Esta oferta não permite editar a contribuição de referência.");
        ride.ReferenceAmount=dto.ReferenceAmount;await _context.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Ok(new{message="Referência atualizada. Acordos anteriores foram preservados."});
    }

    // GET: api/rides?destination=Campus&date=2026-10-01
    [HttpGet]
    public async Task<IActionResult> GetRides([FromQuery] string? destination, [FromQuery] DateTime? date, [FromQuery] string? vehicleType)
    {
        var query = _context.Rides.AsQueryable();
        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            var type = vehicleType.Trim().ToUpperInvariant();
            if (type is not ("CAR" or "MOTO")) return BadRequest(new { message = "Tipo de veículo inválido." });
            query = query.Where(r => r.VehicleType == type);
        }

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
                r.VehicleType,
                r.PricingMode,
                r.ReferenceAmount,
                DriverName = r.Driver!.Name,
                DriverPhone = r.Driver.Phone
            })
            .ToListAsync();

        return Ok(rides);
    }

    // POST: api/rides/1/join
    [HttpPost("{id}/join")]
    [Authorize]
    public async Task<IActionResult> JoinRide(int id,CancellationToken ct)
        => Ok(await _negotiations.ReserveFreeAsync(TravelRules.Actor(User),id,ct));

    [HttpGet("reservations")]
    [Authorize]
    public async Task<IActionResult> Reservations()
    {
        var claim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        if (!int.TryParse(claim?.Value, out var passengerId)) return Unauthorized();
        var reservations = await _context.RideReservations.Where(r => r.PassengerId == passengerId)
            .OrderBy(r => r.Ride.DepartureTime < DateTime.Now).ThenBy(r => r.Ride.DepartureTime)
            .Select(r => new {
                r.Ride.Id, r.Ride.Origin, r.Ride.Destination, r.Ride.DepartureTime,
                r.Ride.AvailableSeats, r.Ride.VehicleType,
                DriverName = r.Ride.Driver!.Name, DriverPhone = r.Ride.Driver.Phone,
                r.AgreedAmount, r.PricingMode, r.Currency,
                IsReserved = true
            }).ToListAsync();
        return Ok(reservations);
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
    public Guid RouteQuoteId { get; set; }
    public PricingMode PricingMode { get; set; }
    public decimal? ReferenceAmount { get; set; }
    public string VehicleType { get; set; } = "CAR";
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