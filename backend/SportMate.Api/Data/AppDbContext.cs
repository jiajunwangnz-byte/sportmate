using Microsoft.EntityFrameworkCore;
using SportMate.Api.Models;

namespace SportMate.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Club> Clubs => Set<Club>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<AppUser>();
        user.Property(x => x.Name).HasMaxLength(40);
        user.Property(x => x.Email).HasMaxLength(120);
        user.Property(x => x.Hint).HasMaxLength(80);
        user.Property(x => x.HintEn).HasMaxLength(120);
        user.Property(x => x.Gender).HasConversion<string>().HasMaxLength(16);
        user.Property(x => x.SkillLevel).HasPrecision(3, 1);

        var venue = modelBuilder.Entity<Venue>();
        venue.Property(x => x.Name).HasMaxLength(80);
        venue.Property(x => x.Address).HasMaxLength(160);
        venue.Property(x => x.Suburb).HasMaxLength(40);
        venue.Property(x => x.Area).HasMaxLength(40);
        venue.Property(x => x.Sports).HasMaxLength(80);
        venue.Property(x => x.OpensAt).HasMaxLength(5);
        venue.Property(x => x.ClosesAt).HasMaxLength(5);
        venue.Property(x => x.HourlyRate).HasPrecision(8, 2);

        var club = modelBuilder.Entity<Club>();
        club.Property(x => x.Name).HasMaxLength(40);
        club.Property(x => x.Bio).HasMaxLength(600);
        club.Property(x => x.Monogram).HasMaxLength(4);
        club.Property(x => x.Color).HasMaxLength(16);
        club.Property(x => x.StripeAccountId).HasMaxLength(80);
        club.Property(x => x.Sport).HasConversion<string>().HasMaxLength(24);
        club.Property(x => x.Rating).HasPrecision(2, 1);
        club.Property(x => x.ReturnRate).HasPrecision(4, 3);
        club.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        club.HasOne(x => x.HomeVenue).WithMany().HasForeignKey(x => x.HomeVenueId).OnDelete(DeleteBehavior.SetNull);

        var activity = modelBuilder.Entity<Activity>();
        activity.Property(x => x.Title).HasMaxLength(80);
        activity.Property(x => x.TitleEn).HasMaxLength(80);
        activity.Property(x => x.CourtNumbers).HasMaxLength(40);
        activity.Property(x => x.Shuttlecock).HasMaxLength(60);
        activity.Property(x => x.RefundPolicy).HasMaxLength(16);
        activity.Property(x => x.Sport).HasConversion<string>().HasMaxLength(24);
        activity.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        activity.Property(x => x.Price).HasPrecision(8, 2);
        activity.Property(x => x.WomenDiscount).HasPrecision(8, 2);
        activity.Property(x => x.SkillMin).HasPrecision(3, 1);
        activity.Property(x => x.SkillMax).HasPrecision(3, 1);
        activity.HasIndex(x => new { x.Sport, x.StartAt });
        activity.HasOne(x => x.Club).WithMany(x => x.Activities).HasForeignKey(x => x.ClubId).OnDelete(DeleteBehavior.Restrict);
        activity.HasOne(x => x.Venue).WithMany().HasForeignKey(x => x.VenueId).OnDelete(DeleteBehavior.Restrict);
        activity.HasOne(x => x.Organizer).WithMany().HasForeignKey(x => x.OrganizerId).OnDelete(DeleteBehavior.Restrict);

        var registration = modelBuilder.Entity<Registration>();
        registration.Property(x => x.Status).HasConversion<string>().HasMaxLength(24);
        registration.Property(x => x.Amount).HasPrecision(8, 2);
        registration.Property(x => x.CheckoutSessionId).HasMaxLength(120);
        registration.Property(x => x.TransferCode).HasMaxLength(16);
        registration.HasIndex(x => x.TransferCode).IsUnique().HasFilter("\"TransferCode\" IS NOT NULL");
        registration.HasOne(x => x.Activity).WithMany(x => x.Registrations).HasForeignKey(x => x.ActivityId).OnDelete(DeleteBehavior.Cascade);
        registration.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        var payment = modelBuilder.Entity<Payment>();
        payment.Property(x => x.Provider).HasConversion<string>().HasMaxLength(16);
        payment.Property(x => x.Status).HasMaxLength(24);
        payment.Property(x => x.Currency).HasMaxLength(8);
        payment.Property(x => x.CheckoutSessionId).HasMaxLength(120);
        payment.Property(x => x.PaymentIntentId).HasMaxLength(120);
        payment.HasOne(x => x.Registration).WithMany().HasForeignKey(x => x.RegistrationId).OnDelete(DeleteBehavior.Cascade);
    }
}
