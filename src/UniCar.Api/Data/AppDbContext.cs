using Microsoft.EntityFrameworkCore;
using UniCar.Api.Domain;

namespace UniCar.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<University> Universities => Set<University>();
    public DbSet<UniversityDomain> UniversityDomains => Set<UniversityDomain>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<User> Users => Set<User>();
    public DbSet<SavedPlace> SavedPlaces => Set<SavedPlace>();
    public DbSet<EmailVerificationCode> EmailVerificationCodes => Set<EmailVerificationCode>();

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
            e.HasOne(x => x.University).WithMany().HasForeignKey(x => x.UniversityId);
            e.HasMany(x => x.SavedPlaces).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<SavedPlace>(e =>
        {
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.Label).HasMaxLength(60);
            e.Property(x => x.Address).HasMaxLength(300);
        });
        b.Entity<EmailVerificationCode>(e =>
        {
            e.Property(x => x.Email).HasMaxLength(254);
            e.HasIndex(x => new { x.Email, x.CreatedAt });
        });
    }
}
