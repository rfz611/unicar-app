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
    public DbSet<RideReservation> RideReservations { get; set; }
    public DbSet<FareNegotiation> FareNegotiations { get; set; }
    public DbSet<FareProposal> FareProposals { get; set; }
    public DbSet<Rating> Ratings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Ride>().Property(r => r.PricingMode).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<RideReservation>().Property(r => r.PricingMode).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<RideReservation>().Property(r => r.Currency).HasMaxLength(3);
        modelBuilder.Entity<RideReservation>().HasOne<FareNegotiation>().WithMany().HasForeignKey(r => r.NegotiationId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FareNegotiation>().HasIndex(n => new { n.RideId, n.PassengerId }).IsUnique();
        modelBuilder.Entity<FareNegotiation>().Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<FareNegotiation>().Property(n => n.LatestAmount).HasPrecision(10, 2);
        modelBuilder.Entity<FareNegotiation>().HasOne(n => n.Ride).WithMany().HasForeignKey(n => n.RideId);
        modelBuilder.Entity<FareNegotiation>().HasOne(n => n.Passenger).WithMany().HasForeignKey(n => n.PassengerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FareProposal>().Property(p => p.Amount).HasPrecision(10, 2);
        modelBuilder.Entity<FareProposal>().HasOne(p => p.Author).WithMany().HasForeignKey(p => p.AuthorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FareProposal>().HasOne(p => p.Negotiation).WithMany().HasForeignKey(p => p.NegotiationId);

        modelBuilder.Entity<RideReservation>().HasIndex(r => new { r.RideId, r.PassengerId }).IsUnique();
        modelBuilder.Entity<RideReservation>().HasOne(r => r.Ride).WithMany().HasForeignKey(r => r.RideId);
        modelBuilder.Entity<RideReservation>().HasOne<User>().WithMany().HasForeignKey(r => r.PassengerId);

        // Uma CNH só pode estar vinculada a uma conta.
        modelBuilder.Entity<User>()
            .HasIndex(u => u.CnhNumber)
            .IsUnique();
    }
}
