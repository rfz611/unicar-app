using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniCar.API.Models;

[Table("rides")]
public class Ride
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("driver_id")]
    public int DriverId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("origin")]
    public string Origin { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("destination")]
    public string Destination { get; set; } = string.Empty;

    [Required]
    [Column("departure_time")]
    public DateTime DepartureTime { get; set; }

    [Required]
    [Column("available_seats")]
    public int AvailableSeats { get; set; }

    [Column("vehicle_type")]
    [MaxLength(4)]
    public string VehicleType { get; set; } = "CAR";

    [Column("pricing_mode")]
    public PricingMode PricingMode { get; set; } = PricingMode.LegacyUnknown;
    [Column("reference_amount", TypeName = "decimal(10,2)")]
    public decimal? ReferenceAmount { get; set; }
    [Column("original_seats")]
    public int OriginalSeats { get; set; }
    [Column("origin_latitude")] public double? OriginLatitude { get; set; }
    [Column("origin_longitude")] public double? OriginLongitude { get; set; }
    [Column("destination_latitude")] public double? DestinationLatitude { get; set; }
    [Column("destination_longitude")] public double? DestinationLongitude { get; set; }
    [Column("origin_place_id")] public string? OriginPlaceId { get; set; }
    [Column("destination_place_id")] public string? DestinationPlaceId { get; set; }
    [Column("pricing_config_version")] public string? PricingConfigVersion { get; set; }

    [ForeignKey("DriverId")]
    public User? Driver { get; set; }
}