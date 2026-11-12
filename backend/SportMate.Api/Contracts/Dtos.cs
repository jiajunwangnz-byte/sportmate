using SportMate.Api.Models;

namespace SportMate.Api.Contracts;

public record MetaDto(
    string City,
    string Currency,
    decimal PlatformFeePercent,
    bool StripeEnabled,
    string? StripePublishableKey,
    string PaymentMode,
    DateOnly Today,
    string Timezone,
    DateOnly SuggestedDate);

public record UserDto(
    Guid Id,
    string Name,
    string Gender,
    decimal SkillLevel,
    bool IsNewcomer,
    string? AvatarUrl,
    bool Listed,
    string? Hint,
    string? HintEn);

public record VenueBriefDto(
    Guid Id,
    string Name,
    string Address,
    string Suburb,
    string Area,
    double DistanceKm,
    double MapX,
    double MapY);

public record ClubBriefDto(
    Guid Id,
    string Name,
    string Monogram,
    string Color,
    string? AvatarUrl,
    int EventsHosted,
    decimal Rating,
    decimal ReturnRate);

public record ParticipantDto(
    Guid UserId,
    string Name,
    string Gender,
    decimal SkillLevel,
    bool IsNewcomer,
    bool IsOrganizer,
    string Status);

public record ActivityDto(
    Guid Id,
    string Title,
    string TitleEn,
    string Sport,
    string Status,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Capacity,
    int Joined,
    int MaleCount,
    int FemaleCount,
    int Waitlist,
    int SpotsLeft,
    decimal Price,
    decimal WomenDiscount,
    decimal SkillMin,
    decimal SkillMax,
    bool CourtsBooked,
    int? CourtsUsed,
    int? PlayersPerCourt,
    string? CourtNumbers,
    string? Shuttlecock,
    string RefundPolicy,
    bool ParkingFree,
    bool WomenFriendly,
    bool WelcomeNewcomers,
    bool Competitive,
    bool Casual,
    VenueBriefDto Venue,
    ClubBriefDto Club,
    string? MyStatus,
    Guid? MyRegistrationId,
    bool FromTransfer,
    IReadOnlyList<ParticipantDto> Participants);

public record SessionBriefDto(
    Guid Id,
    string Title,
    string TitleEn,
    string Sport,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    int Joined,
    int Capacity,
    int SpotsLeft,
    decimal Price,
    decimal SkillMin,
    decimal SkillMax);

public record VenueDto(
    Guid Id,
    string Name,
    string Address,
    string Suburb,
    string Area,
    double DistanceKm,
    double Latitude,
    double Longitude,
    double MapX,
    double MapY,
    int CourtCount,
    decimal? HourlyRate,
    bool WalkInBooking,
    bool IsOpen,
    string OpensAt,
    string ClosesAt,
    IReadOnlyList<string> Sports,
    int SessionsOnDate,
    IReadOnlyList<SessionBriefDto> Sessions);

public record AreaCountDto(string Area, int Sessions, bool WalkIn);

public record VenueListDto(IReadOnlyList<AreaCountDto> Areas, IReadOnlyList<VenueDto> Venues);

public record ClubDto(
    Guid Id,
    string Name,
    string? Bio,
    string Sport,
    string Monogram,
    string Color,
    string? AvatarUrl,
    Guid OwnerId,
    string OwnerName,
    Guid? HomeVenueId,
    string? HomeVenueName,
    int EventsHosted,
    decimal Rating,
    decimal ReturnRate,
    bool StripeConnected,
    bool StripeDemo,
    bool Mine,
    IReadOnlyList<SessionBriefDto> Upcoming);

public record MyClubDto(
    Guid Id,
    string Name,
    string Sport,
    string Monogram,
    string Color,
    string? AvatarUrl,
    bool StripeConnected,
    bool StripeDemo,
    int EventsHosted,
    decimal Rating);

public record MyRegistrationDto(
    Guid Id,
    Guid ActivityId,
    string ActivityTitle,
    string ActivityTitleEn,
    DateTimeOffset StartAt,
    string VenueName,
    string Status,
    decimal Amount,
    bool FromTransfer,
    string Sport);

public record MeDto(
    UserDto User,
    IReadOnlyList<MyClubDto> Clubs,
    IReadOnlyList<MyRegistrationDto> Registrations,
    IReadOnlyList<ActivityDto> Drafts);

public record UpsertActivityRequest(
    Guid ClubId,
    Guid VenueId,
    string Title,
    string? TitleEn,
    DateOnly Date,
    string StartTime,
    string EndTime,
    int Capacity,
    decimal Price,
    decimal WomenDiscount,
    decimal SkillMin,
    decimal SkillMax,
    bool CourtsBooked,
    int? CourtsUsed,
    int? PlayersPerCourt,
    string? CourtNumbers,
    string? Shuttlecock,
    string RefundPolicy,
    bool ParkingFree,
    bool WomenFriendly,
    bool WelcomeNewcomers,
    bool Competitive,
    bool Casual,
    bool Publish);

public record SaveActivityResult(
    ActivityDto Activity,
    string ShareUrl,
    decimal EstimatedGross,
    decimal PlatformFee,
    decimal EstimatedNet);

public record CreateClubRequest(
    string Name,
    string? Bio,
    string Sport,
    Guid? HomeVenueId,
    string? Monogram,
    string? Color,
    string? AvatarUrl,
    bool AcceptedTerms);

public record UpdateMeRequest(string? Name, decimal? SkillLevel, string? Gender);

public record JoinResult(
    Guid RegistrationId,
    string Status,
    decimal Amount,
    int? WaitlistPosition,
    string PaymentMode,
    string? CheckoutUrl);

public record DemoPayRequest(Guid RegistrationId);

public record ConfirmPayRequest(string SessionId);

public record TransferResult(string Code, string Url);

public record ClaimRequest(string Code);

public record ConnectResult(string Mode, string? Url, bool Connected);

public record DayCountDto(DateOnly Date, int Count);
