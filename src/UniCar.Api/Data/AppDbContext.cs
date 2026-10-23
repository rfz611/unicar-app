using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UniCar.Api.Domain;

namespace UniCar.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<University> Universities => Set<University>();
    public DbSet<UniversityDomain> UniversityDomains => Set<UniversityDomain>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();
    public DbSet<DriverApplication> DriverApplications => Set<DriverApplication>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<TimeSuggestion> TimeSuggestions => Set<TimeSuggestion>();
    public DbSet<RouteAlert> RouteAlerts => Set<RouteAlert>();
    public DbSet<RideRequest> RideRequests => Set<RideRequest>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripPassenger> TripPassengers => Set<TripPassenger>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<TripShare> TripShares => Set<TripShare>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<University>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.ShortName).HasMaxLength(40);
            e.HasMany(x => x.Domains).WithOne().HasForeignKey(x => x.UniversityId);
            e.HasMany(x => x.Campuses).WithOne().HasForeignKey(x => x.UniversityId);
        });
        b.Entity<UniversityDomain>(e =>
        {
            e.Property(x => x.Domain).HasMaxLength(120);
            e.HasIndex(x => x.Domain).IsUnique();
        });
        b.Entity<Campus>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(160);
            e.Property(x => x.Address).HasMaxLength(300);
        });
        b.Entity<User>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.CpfHash).IsUnique();
            e.Property(x => x.FullName).HasMaxLength(120);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.Course).HasMaxLength(80);
            e.Property(x => x.Bio).HasMaxLength(140);
            e.Property(x => x.Gender).HasMaxLength(30);
            e.Property(x => x.Shift).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DriverStatus).HasConversion<string>().HasMaxLength(12);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(10);
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasOne(x => x.Campus).WithMany().HasForeignKey(x => x.CampusId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.SavedPlaces).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Preferences).WithOne().HasForeignKey<UserPreferences>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Ignore(x => x.MainCampus);
        });
        b.Entity<UserPreferences>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Theme).HasConversion<string>().HasMaxLength(10);
        });
        b.Entity<SavedPlace>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Label).HasMaxLength(60);
            e.Property(x => x.Address).HasMaxLength(300);
            e.Property(x => x.Neighborhood).HasMaxLength(120);
            e.Property(x => x.GooglePlaceId).HasMaxLength(300);
            // Uma casa por pessoa (Kind = Home); Work/Other podem repetir.
            e.HasIndex(x => x.UserId).HasFilter("\"Kind\" = 'Home'").IsUnique();
        });
        b.Entity<DriverApplication>(e =>
        {
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.CnhHash).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.CnhHash).HasMaxLength(64);
            e.Property(x => x.Plate).HasMaxLength(7);
            e.Property(x => x.VehicleModel).HasMaxLength(60);
            e.Property(x => x.VehicleColor).HasMaxLength(30);
            e.Property(x => x.CnhPhotoFile).HasMaxLength(100);
            e.Property(x => x.SelfieFile).HasMaxLength(100);
            e.Property(x => x.RejectionReason).HasMaxLength(300);
            e.Ignore(x => x.IsComplete);
        });
        b.Entity<Vehicle>(e =>
        {
            e.Property(x => x.Plate).HasMaxLength(7);
            e.Property(x => x.Model).HasMaxLength(60);
            e.Property(x => x.Color).HasMaxLength(30);
            e.HasIndex(x => x.Plate).HasFilter("\"Active\"").IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Ride>(e =>
        {
            e.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.OwnsOne(x => x.Origin, ConfigurePlace);
            e.OwnsOne(x => x.Destination, ConfigurePlace);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
            e.Property(x => x.PricePerSeat).HasPrecision(10, 2);
            e.Property(x => x.DriverAmountPerSeat).HasPrecision(10, 2);
            e.HasIndex(x => new { x.Status, x.DepartureAt });
            e.HasIndex(x => x.DriverId);
            e.Ignore(x => x.SeatsLeft);
        });
        b.Entity<Reservation>(e =>
        {
            e.HasOne(x => x.Ride).WithMany(x => x.Reservations).HasForeignKey(x => x.RideId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Passenger).WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
            e.Property(x => x.Price).HasPrecision(10, 2);
            // Uma reserva ativa por pessoa por carona, garantido pelo banco mesmo com dois cliques ao mesmo tempo.
            e.HasIndex(x => new { x.RideId, x.PassengerId }).HasFilter("\"Status\" = 'Confirmed'").IsUnique();
            e.HasIndex(x => x.PassengerId);
        });
        b.Entity<TimeSuggestion>(e =>
        {
            e.HasOne(x => x.Ride).WithMany().HasForeignKey(x => x.RideId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Passenger).WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
        });
        b.Entity<RouteAlert>(e =>
        {
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Label).HasMaxLength(300);
            e.HasIndex(x => x.UserId);
        });
        b.Entity<RideRequest>(e =>
        {
            e.HasOne(x => x.Passenger).WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
            e.OwnsOne(x => x.Origin, ConfigurePlace);
            e.OwnsOne(x => x.Destination, ConfigurePlace);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(12);
            e.Property(x => x.Price).HasPrecision(10, 2);
            e.Property(x => x.DriverAmount).HasPrecision(10, 2);
            e.HasIndex(x => new { x.Status, x.CreatedAt });
            e.HasIndex(x => x.PassengerId);
        });
        b.Entity<Trip>(e =>
        {
            e.HasOne(x => x.Driver).WithMany().HasForeignKey(x => x.DriverId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Ride>().WithMany().HasForeignKey(x => x.RideId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<RideRequest>().WithMany().HasForeignKey(x => x.RideRequestId).OnDelete(DeleteBehavior.SetNull);
            e.OwnsOne(x => x.Origin, ConfigurePlace);
            e.OwnsOne(x => x.Destination, ConfigurePlace);
            e.Property(x => x.Mode).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(14);
            // uma corrida por carona marcada
            e.HasIndex(x => x.RideId).IsUnique();
            e.HasIndex(x => new { x.DriverId, x.Status });
            e.Ignore(x => x.IsActive);
        });
        b.Entity<TripPassenger>(e =>
        {
            e.HasKey(x => new { x.TripId, x.PassengerId });
            e.HasOne(x => x.Trip).WithMany(x => x.Passengers).HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Passenger).WithMany().HasForeignKey(x => x.PassengerId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Amount).HasPrecision(10, 2);
            e.Property(x => x.DriverAmount).HasPrecision(10, 2);
            e.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(6);
            e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.PassengerId);
        });
        b.Entity<Rating>(e =>
        {
            e.HasOne<Trip>().WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TripId, x.FromUserId, x.ToUserId }).IsUnique();
            e.HasIndex(x => x.ToUserId);
        });
        b.Entity<Message>(e =>
        {
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Text).HasMaxLength(500);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(8);
            e.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        });
        b.Entity<TripShare>(e =>
        {
            e.HasOne(x => x.Trip).WithMany().HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.TokenHash).HasMaxLength(64);
            e.HasIndex(x => x.TokenHash).IsUnique();
        });
        b.Entity<EmailVerificationCode>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => new { x.Email, x.CreatedAt });
        });
    }

    private static void ConfigurePlace<T>(OwnedNavigationBuilder<T, RidePlace> p) where T : class
    {
        p.Property(x => x.Address).HasMaxLength(300);
        p.Property(x => x.Neighborhood).HasMaxLength(120);
    }
}
