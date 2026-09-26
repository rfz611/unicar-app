using Microsoft.EntityFrameworkCore;
using UniCar.API.Models;

namespace UniCar.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Ride> Rides { get; set; }
}
