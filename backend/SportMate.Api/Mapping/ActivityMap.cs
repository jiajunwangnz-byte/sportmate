using Microsoft.EntityFrameworkCore;
using SportMate.Api.Contracts;
using SportMate.Api.Infrastructure;
using SportMate.Api.Models;

namespace SportMate.Api.Mapping;

public static class ActivityMap
{
    public static IQueryable<Activity> Ready(this IQueryable<Activity> query) =>
        query
            .Include(a => a.Venue)
            .Include(a => a.Club)
            .Include(a => a.Registrations)
            .ThenInclude(r => r.User);

    public static int Holding(Activity activity, DateTimeOffset now) =>
        activity.Registrations.Count(r =>
            r.Status == RegistrationStatus.Paid ||
            (r.Status == RegistrationStatus.PendingPayment && r.CreatedAt >= now.AddMinutes(-15)));

    public static ActivityDto ToDto(Activity activity, Guid? me, DateTimeOffset now)
    {
        var paid = activity.Registrations.Where(r => r.Status == RegistrationStatus.Paid).ToList();
        var wait = activity.Registrations
            .Where(r => r.Status == RegistrationStatus.Waitlist)
            .OrderBy(r => r.WaitlistPosition)
            .ToList();
        var mine = activity.Registrations
            .Where(r => me != null && r.UserId == me && r.Status is RegistrationStatus.Paid or RegistrationStatus.PendingPayment or RegistrationStatus.Waitlist)
            .OrderBy(r => r.Status == RegistrationStatus.Paid ? 0 : r.Status == RegistrationStatus.Waitlist ? 2 : 1)
            .FirstOrDefault();

        var people = paid.Concat(wait)
            .OrderBy(r => r.Status != RegistrationStatus.Paid)
            .ThenBy(r => r.UserId != activity.OrganizerId)
            .ThenBy(r => r.CreatedAt)
            .Select(r => new ParticipantDto(
                r.UserId,
                r.User.Name,
                r.User.Gender.ToString(),
                r.User.SkillLevel,
                r.User.IsNewcomer,
                r.UserId == activity.OrganizerId,
                r.Status.ToString()))
            .ToList();

        return new ActivityDto(
            activity.Id,
            activity.Title,
            activity.TitleEn,
            activity.Sport.ToString(),
            activity.Status.ToString(),
            activity.StartAt,
            activity.EndAt,
            activity.Capacity,
            paid.Count,
            paid.Count(r => r.User.Gender == Gender.Male),
            paid.Count(r => r.User.Gender == Gender.Female),
            wait.Count,
            Math.Max(0, activity.Capacity - Holding(activity, now)),
            activity.Price,
            activity.WomenDiscount,
            activity.SkillMin,
            activity.SkillMax,
            activity.CourtsBooked,
            activity.CourtsUsed,
            activity.PlayersPerCourt,
            activity.CourtNumbers,
            activity.Shuttlecock,
            activity.RefundPolicy,
            activity.ParkingFree,
            activity.WomenFriendly,
            activity.WelcomeNewcomers,
            activity.Competitive,
            activity.Casual,
            VenueBrief(activity.Venue),
            ClubBrief(activity.Club),
            mine?.Status.ToString(),
            mine?.Id,
            mine?.FromTransfer ?? false,
            people);
    }

    public static VenueBriefDto VenueBrief(Venue venue) => new(
        venue.Id, venue.Name, venue.Address, venue.Suburb, venue.Area,
        venue.DistanceKm, venue.MapX, venue.MapY);

    public static ClubBriefDto ClubBrief(Club club) => new(
        club.Id, club.Name, club.Monogram, club.Color, club.AvatarUrl,
        club.EventsHosted, club.Rating, club.ReturnRate);

    public static SessionBriefDto Brief(Activity activity, DateTimeOffset now)
    {
        var paid = activity.Registrations.Count(r => r.Status == RegistrationStatus.Paid);
        return new SessionBriefDto(
            activity.Id,
            activity.Title,
            activity.TitleEn,
            activity.Sport.ToString(),
            activity.StartAt,
            activity.EndAt,
            paid,
            activity.Capacity,
            Math.Max(0, activity.Capacity - Holding(activity, now)),
            activity.Price,
            activity.SkillMin,
            activity.SkillMax);
    }

    public static UserDto User(AppUser user) => new(
        user.Id, user.Name, user.Gender.ToString(), user.SkillLevel, user.IsNewcomer,
        user.AvatarUrl, user.Listed, user.Hint, user.HintEn);

    public static bool IsOpen(Venue venue, DateTimeOffset now)
    {
        var local = AucklandTime.Now();
        if (!TimeOnly.TryParse(venue.OpensAt, out var open) || !TimeOnly.TryParse(venue.ClosesAt, out var close))
            return true;
        var clock = TimeOnly.FromDateTime(local.DateTime);
        return clock >= open && clock < close;
    }
}
