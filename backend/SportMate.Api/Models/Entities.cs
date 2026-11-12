namespace SportMate.Api.Models;

public enum SportKind { Badminton, Pickleball, Tennis, TableTennis }

public enum Gender { Female, Male, Other }

public enum ActivityStatus { Draft, Published, Cancelled }

public enum RegistrationStatus { PendingPayment, Paid, Waitlist, Cancelled, Transferred }

public enum PaymentProvider { Demo, Stripe }

public class AppUser
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Gender Gender { get; set; }
    public decimal SkillLevel { get; set; }
    public bool IsNewcomer { get; set; }
    public bool Listed { get; set; }
    public string? Hint { get; set; }
    public string? HintEn { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Venue
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Suburb { get; set; } = "";
    public string Area { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceKm { get; set; }
    public double MapX { get; set; }
    public double MapY { get; set; }
    public int CourtCount { get; set; }
    public decimal? HourlyRate { get; set; }
    public bool WalkInBooking { get; set; }
    public string Sports { get; set; } = "";
    public string OpensAt { get; set; } = "09:00";
    public string ClosesAt { get; set; } = "22:30";
}

public class Club
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public AppUser Owner { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Bio { get; set; }
    public SportKind Sport { get; set; }
    public Guid? HomeVenueId { get; set; }
    public Venue? HomeVenue { get; set; }
    public string Monogram { get; set; } = "局";
    public string Color { get; set; } = "#0f7a45";
    public string? AvatarUrl { get; set; }
    public int EventsHosted { get; set; }
    public decimal Rating { get; set; }
    public decimal ReturnRate { get; set; }
    public string? StripeAccountId { get; set; }
    public bool StripeChargesEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}

public class Activity
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;
    public Guid VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public Guid OrganizerId { get; set; }
    public AppUser Organizer { get; set; } = null!;
    public SportKind Sport { get; set; }
    public string Title { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public ActivityStatus Status { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int Capacity { get; set; }
    public decimal Price { get; set; }
    public decimal WomenDiscount { get; set; }
    public decimal SkillMin { get; set; }
    public decimal SkillMax { get; set; }
    public bool CourtsBooked { get; set; }
    public int? CourtsUsed { get; set; }
    public int? PlayersPerCourt { get; set; }
    public string? CourtNumbers { get; set; }
    public string? Shuttlecock { get; set; }
    public string RefundPolicy { get; set; } = "h24";
    public bool ParkingFree { get; set; }
    public bool WomenFriendly { get; set; }
    public bool WelcomeNewcomers { get; set; }
    public bool Competitive { get; set; }
    public bool Casual { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}

public class Registration
{
    public Guid Id { get; set; }
    public Guid ActivityId { get; set; }
    public Activity Activity { get; set; } = null!;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public RegistrationStatus Status { get; set; }
    public decimal Amount { get; set; }
    public int? WaitlistPosition { get; set; }
    public bool FromTransfer { get; set; }
    public string? CheckoutSessionId { get; set; }
    public string? TransferCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid RegistrationId { get; set; }
    public Registration Registration { get; set; } = null!;
    public PaymentProvider Provider { get; set; }
    public string Status { get; set; } = "pending";
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "nzd";
    public string? CheckoutSessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
