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

    [ForeignKey("DriverId")]
    public User? Driver { get; set; }
}