using Microsoft.EntityFrameworkCore;
using SportMate.Api.Contracts;
using SportMate.Api.Data;
using SportMate.Api.Infrastructure;
using SportMate.Api.Mapping;
using SportMate.Api.Models;
using SportMate.Api.Services;

namespace SportMate.Api.Controllers;

[ApiController]
[Route("api")]
public class CatalogController(AppDbContext db, CurrentUserAccessor current, StripeGateway stripe, IConfiguration config) : ControllerBase
{
    DateTimeOffset Now => DateTimeOffset.UtcNow;
    AppUser RequireUser() => current.User ?? throw new ApiException(401, "请先选择一个演示账号");

    [HttpGet("meta")]
    public IActionResult Meta()
    {
        var today = AucklandTime.Today();
        var local = AucklandTime.Now();
        var suggested = AucklandTime.Next(today, DayOfWeek.Sunday);
        if (suggested == today && local.Hour >= 16)
            suggested = today.AddDays(7);
        return Ok(new MetaDto(
            "Auckland",
            "NZD",
            config.GetValue("PlatformFeePercent", 8m),
            stripe.Enabled,
            stripe.PublishableKey,
            stripe.Enabled ? "stripe" : "demo",
            today,
            "Pacific/Auckland",
            suggested));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var users = await db.Users.AsNoTracking().Where(u => u.Listed).OrderBy(u => u.Name).ToListAsync();
        return Ok(users.Select(ActivityMap.User));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var me = RequireUser();
        var clubs = await db.Clubs.AsNoTracking().Where(c => c.OwnerId == me.Id).OrderBy(c => c.Name).ToListAsync();
        var regs = await db.Registrations.AsNoTracking()
            .Include(r => r.Activity).ThenInclude(a => a.Venue)
            .Where(r => r.UserId == me.Id && r.Status != RegistrationStatus.Cancelled)
            .OrderByDescending(r => r.Activity.StartAt)
            .ToListAsync();
        var drafts = await db.Activities.AsNoTracking().Ready()
            .Where(a => a.OrganizerId == me.Id && a.Status == ActivityStatus.Draft)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return Ok(new MeDto(
            ActivityMap.User(me),
            clubs.Select(c => new MyClubDto(
                c.Id, c.Name, c.Sport.ToString(), c.Monogram, c.Color, c.AvatarUrl,
                c.StripeChargesEnabled, c.StripeAccountId == "demo", c.EventsHosted, c.Rating)).ToList(),
            regs.Select(r => new MyRegistrationDto(
                r.Id, r.ActivityId, r.Activity.Title, r.Activity.TitleEn, r.Activity.StartAt,
                r.Activity.Venue.Name, r.Status.ToString(), r.Amount, r.FromTransfer, r.Activity.Sport.ToString())).ToList(),
            drafts.Select(a => ActivityMap.ToDto(a, me.Id, Now)).ToList()));
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateMeRequest request)
    {
        var me = RequireUser();
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (name.Length is < 1 or > 40)
                throw new ApiException(400, "名字长度不对");
            me.Name = name;
        }
        if (request.SkillLevel is decimal skill)
        {
            if (!Fees.HalfStep(skill))
                throw new ApiException(400, "水平用 L1 到 L8，步长 0.5");
            me.SkillLevel = skill;
        }
        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            if (!Enum.TryParse<Gender>(request.Gender, true, out var gender))
                throw new ApiException(400, "性别不正确");
            me.Gender = gender;
        }
        await db.SaveChangesAsync();
        return await Me();
    }

    [HttpGet("venues")]
    public async Task<IActionResult> Venues([FromQuery] DateOnly? date, [FromQuery] string? sport, [FromQuery] bool? walkIn, [FromQuery] bool? hasSessions)
    {
        var day = date ?? AucklandTime.Today();
        var start = AucklandTime.At(day, TimeOnly.MinValue);
        var venues = await db.Venues.AsNoTracking().OrderBy(v => v.DistanceKm).ToListAsync();
        var activities = await db.Activities.AsNoTracking().Ready()
            .Where(a => a.Status == ActivityStatus.Published && a.StartAt >= start && a.StartAt < start.AddDays(1))
            .ToListAsync();
        if (!string.IsNullOrWhiteSpace(sport) && Enum.TryParse<SportKind>(sport, true, out var kind))
        {
            activities = activities.Where(a => a.Sport == kind).ToList();
            venues = venues.Where(v => v.Sports.Split(',', StringSplitOptions.TrimEntries).Contains(kind.ToString())).ToList();
        }

        var areas = venues
            .GroupBy(v => v.Area)
            .Select(g => new AreaCountDto(
                g.Key,
                activities.Count(a => g.Any(v => v.Id == a.VenueId)),
                g.Any(v => v.WalkInBooking)))
            .OrderByDescending(a => a.Sessions)
            .ThenBy(a => a.Area)
            .ToList();

        IEnumerable<Venue> filtered = venues;
        if (walkIn == true)
            filtered = filtered.Where(v => v.WalkInBooking);
        if (hasSessions == true)
            filtered = filtered.Where(v => activities.Any(a => a.VenueId == v.Id));

        var dtos = filtered.Select(v =>
        {
            var sessions = activities.Where(a => a.VenueId == v.Id).OrderBy(a => a.StartAt).ToList();
            return new VenueDto(
                v.Id, v.Name, v.Address, v.Suburb, v.Area, v.DistanceKm, v.Latitude, v.Longitude,
                v.MapX, v.MapY, v.CourtCount, v.HourlyRate, v.WalkInBooking, ActivityMap.IsOpen(v, Now),
                v.OpensAt, v.ClosesAt,
                v.Sports.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                sessions.Count,
                sessions.Select(a => ActivityMap.Brief(a, Now)).ToList());
        }).ToList();
        return Ok(new VenueListDto(areas, dtos));
    }

    [HttpGet("venues/{id:guid}")]
    public async Task<IActionResult> Venue(Guid id, [FromQuery] DateOnly? date)
    {
        var venue = await db.Venues.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new ApiException(404, "找不到场馆");
        var day = date ?? AucklandTime.Today();
        var start = AucklandTime.At(day, TimeOnly.MinValue);
        var sessions = await db.Activities.AsNoTracking().Ready()
            .Where(a => a.VenueId == id && a.Status == ActivityStatus.Published && a.StartAt >= start && a.StartAt < start.AddDays(1))
            .OrderBy(a => a.StartAt)
            .ToListAsync();
        return Ok(new VenueDto(
            venue.Id, venue.Name, venue.Address, venue.Suburb, venue.Area, venue.DistanceKm,
            venue.Latitude, venue.Longitude, venue.MapX, venue.MapY, venue.CourtCount, venue.HourlyRate,
            venue.WalkInBooking, ActivityMap.IsOpen(venue, Now), venue.OpensAt, venue.ClosesAt,
            venue.Sports.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            sessions.Count, sessions.Select(a => ActivityMap.Brief(a, Now)).ToList()));
    }

    [HttpGet("clubs")]
    public async Task<IActionResult> Clubs([FromQuery] string? sport)
    {
        var me = current.User?.Id;
        var query = db.Clubs.AsNoTracking().Include(c => c.HomeVenue).Include(c => c.Owner).AsQueryable();
        if (!string.IsNullOrWhiteSpace(sport) && Enum.TryParse<SportKind>(sport, true, out var kind))
            query = query.Where(c => c.Sport == kind);
        var clubs = await query.OrderByDescending(c => c.Rating).ToListAsync();
        var upcoming = await db.Activities.AsNoTracking().Ready()
            .Where(a => a.Status == ActivityStatus.Published && a.StartAt >= Now)
            .OrderBy(a => a.StartAt)
            .ToListAsync();
        return Ok(clubs.Select(c => ToClub(c, me, upcoming.Where(a => a.ClubId == c.Id).Take(4).ToList())));
    }

    [HttpGet("clubs/{id:guid}")]
    public async Task<IActionResult> Club(Guid id)
    {
        var club = await db.Clubs.AsNoTracking().Include(c => c.HomeVenue).Include(c => c.Owner).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new ApiException(404, "找不到俱乐部");
        var upcoming = await db.Activities.AsNoTracking().Ready()
            .Where(a => a.ClubId == id && a.Status == ActivityStatus.Published && a.StartAt >= Now)
            .OrderBy(a => a.StartAt)
            .Take(8)
            .ToListAsync();
        return Ok(ToClub(club, current.User?.Id, upcoming));
    }

    [HttpPost("clubs")]
    public async Task<IActionResult> CreateClub([FromBody] CreateClubRequest request)
    {
        var me = RequireUser();
        if (!request.AcceptedTerms)
            throw new ApiException(400, "请先阅读并同意《组织者守则》");
        if (!Enum.TryParse<SportKind>(request.Sport, true, out var sport))
            throw new ApiException(400, "请选择运动");
        var name = (request.Name ?? "").Trim();
        if (name.Length is < 2 or > 40)
            throw new ApiException(400, "俱乐部名称要在 2 到 40 个字之间");
        Venue? home = null;
        if (request.HomeVenueId is Guid venueId)
        {
            home = await db.Venues.FirstOrDefaultAsync(v => v.Id == venueId)
                ?? throw new ApiException(404, "找不到常驻场馆");
        }
        if (request.AvatarUrl is { Length: > 200_000 })
            throw new ApiException(400, "头像图片太大，请换一张小一点的");
        var monogram = (request.Monogram ?? "").Trim();
        if (monogram.Length == 0)
            monogram = name[..1].ToUpperInvariant();
        if (monogram.Length > 2)
            throw new ApiException(400, "头像文字最多 2 个字");
        var color = string.IsNullOrWhiteSpace(request.Color) ? "#0f7a45" : request.Color.Trim();
        if (color.Length > 16)
            throw new ApiException(400, "颜色不对");
        var bio = (request.Bio ?? "").Trim();
        if (bio.Length > 600)
            throw new ApiException(400, "简介太长了");

        var club = new Club
        {
            Id = Guid.NewGuid(),
            OwnerId = me.Id,
            Name = name,
            Bio = string.IsNullOrEmpty(bio) ? null : bio,
            Sport = sport,
            HomeVenueId = home?.Id,
            Monogram = monogram,
            Color = color,
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl,
            EventsHosted = 0,
            Rating = 5.0m,
            ReturnRate = 0,
            CreatedAt = Now
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();
        club.Owner = me;
        club.HomeVenue = home;
        return Ok(ToClub(club, me.Id, []));
    }

    [HttpPost("clubs/{id:guid}/stripe/connect")]
    public async Task<IActionResult> Connect(Guid id)
    {
        var me = RequireUser();
        if (!stripe.Enabled)
            throw new ApiException(400, "还没有配置 Stripe 密钥");
        var club = await OwnClub(id, me.Id);
        var (accountId, url) = await stripe.StartConnect(club, me.Email);
        club.StripeAccountId = accountId;
        await db.SaveChangesAsync();
        return Ok(new ConnectResult("stripe", url, club.StripeChargesEnabled));
    }

    [HttpPost("clubs/{id:guid}/stripe/demo")]
    public async Task<IActionResult> DemoConnect(Guid id)
    {
        var me = RequireUser();
        if (stripe.Enabled)
            throw new ApiException(400, "已经配置了 Stripe，请走正式开户");
        var club = await OwnClub(id, me.Id);
        club.StripeAccountId = "demo";
        club.StripeChargesEnabled = true;
        await db.SaveChangesAsync();
        return Ok(new ConnectResult("demo", null, true));
    }

    [HttpPost("clubs/{id:guid}/stripe/refresh")]
    public async Task<IActionResult> RefreshStripe(Guid id)
    {
        var me = RequireUser();
        var club = await OwnClub(id, me.Id);
        if (string.IsNullOrWhiteSpace(club.StripeAccountId) || club.StripeAccountId == "demo")
            return Ok(new ConnectResult(club.StripeAccountId == "demo" ? "demo" : "none", null, club.StripeChargesEnabled));
        if (!stripe.Enabled)
            throw new ApiException(400, "还没有配置 Stripe 密钥");
        club.StripeChargesEnabled = await stripe.ChargesEnabled(club.StripeAccountId);
        await db.SaveChangesAsync();
        return Ok(new ConnectResult("stripe", null, club.StripeChargesEnabled));
    }

    async Task<Club> OwnClub(Guid id, Guid userId)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new ApiException(404, "找不到俱乐部");
        if (club.OwnerId != userId)
            throw new ApiException(403, "只能连接自己俱乐部的收款账户");
        return club;
    }

    ClubDto ToClub(Club club, Guid? me, IReadOnlyList<Activity> upcoming) => new(
        club.Id,
        club.Name,
        club.Bio,
        club.Sport.ToString(),
        club.Monogram,
        club.Color,
        club.AvatarUrl,
        club.OwnerId,
        club.Owner.Name,
        club.HomeVenueId,
        club.HomeVenue?.Name,
        club.EventsHosted,
        club.Rating,
        club.ReturnRate,
        club.StripeChargesEnabled,
        club.StripeAccountId == "demo",
        me != null && club.OwnerId == me,
        upcoming.Select(a => ActivityMap.Brief(a, Now)).ToList());
}

[ApiController]
[Route("api/payments")]
public class PaymentsController(BookingService booking) : ControllerBase
{
    [HttpPost("demo")]
    public async Task<IActionResult> Demo([FromBody] DemoPayRequest request)
    {
        await booking.CompleteDemo(request.RegistrationId);
        return NoContent();
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmPayRequest request)
    {
        await booking.ConfirmStripe(request.SessionId);
        return NoContent();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();
        await booking.HandleWebhook(json, Request.Headers["Stripe-Signature"].ToString());
        return Ok();
    }
}
