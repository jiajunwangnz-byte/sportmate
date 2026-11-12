using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SportMate.Api.Contracts;
using SportMate.Api.Data;
using SportMate.Api.Infrastructure;
using SportMate.Api.Mapping;
using SportMate.Api.Models;
using SportMate.Api.Services;

namespace SportMate.Api.Controllers;

[ApiController]
[Route("api/activities")]
public class ActivitiesController(AppDbContext db, CurrentUserAccessor current, BookingService booking, IConfiguration config) : ControllerBase
{
    DateTimeOffset Now => DateTimeOffset.UtcNow;
    AppUser? Me => current.User;
    decimal FeePercent => config.GetValue("PlatformFeePercent", 8m);
    string Frontend => (config["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? sport,
        [FromQuery] DateOnly? date,
        [FromQuery] string? sort,
        [FromQuery] string? area,
        [FromQuery] decimal? skillMin,
        [FromQuery] decimal? skillMax,
        [FromQuery] bool openOnly = false)
    {
        var query = db.Activities.AsNoTracking().Ready().Where(a => a.Status == ActivityStatus.Published);
        if (!string.IsNullOrWhiteSpace(sport) && Enum.TryParse<SportKind>(sport, true, out var kind))
            query = query.Where(a => a.Sport == kind);
        if (date is DateOnly day)
        {
            var start = AucklandTime.At(day, TimeOnly.MinValue);
            query = query.Where(a => a.StartAt >= start && a.StartAt < start.AddDays(1));
        }
        if (!string.IsNullOrWhiteSpace(area) && area != "all")
            query = query.Where(a => a.Venue.Area == area);
        if (skillMin is decimal minLevel)
            query = query.Where(a => a.SkillMax >= minLevel);
        if (skillMax is decimal maxLevel)
            query = query.Where(a => a.SkillMin <= maxLevel);

        var rows = await query.ToListAsync();
        if (openOnly)
            rows = rows.Where(a => ActivityMap.Holding(a, Now) < a.Capacity).ToList();

        rows = sort switch
        {
            "price" => rows.OrderBy(a => a.Price).ThenBy(a => a.StartAt).ToList(),
            "soon" => rows.OrderBy(a => a.StartAt).ToList(),
            _ => rows.OrderBy(a => a.Venue.DistanceKm).ThenBy(a => a.StartAt).ToList()
        };
        return Ok(rows.Select(a => ActivityMap.ToDto(a, Me?.Id, Now)));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] string? sport, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var start = AucklandTime.At(from, TimeOnly.MinValue);
        var end = AucklandTime.At(to.AddDays(1), TimeOnly.MinValue);
        var query = db.Activities.AsNoTracking().Where(a => a.Status == ActivityStatus.Published && a.StartAt >= start && a.StartAt < end);
        if (!string.IsNullOrWhiteSpace(sport) && Enum.TryParse<SportKind>(sport, true, out var kind))
            query = query.Where(a => a.Sport == kind);
        var instants = await query.Select(a => a.StartAt).ToListAsync();
        var counts = instants
            .GroupBy(AucklandTime.DateOf)
            .Select(g => new DayCountDto(g.Key, g.Count()))
            .OrderBy(g => g.Date);
        return Ok(counts);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var activity = await db.Activities.AsNoTracking().Ready().FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new ApiException(404, "找不到这场活动");
        if (activity.Status == ActivityStatus.Draft && activity.OrganizerId != Me?.Id)
            throw new ApiException(404, "找不到这场活动");
        return Ok(ActivityMap.ToDto(activity, Me?.Id, Now));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertActivityRequest request)
    {
        var me = RequireUser();
        var activity = new Activity { Id = Guid.NewGuid(), OrganizerId = me.Id, CreatedAt = Now };
        await Apply(activity, request, me, creating: true);
        db.Activities.Add(activity);
        await db.SaveChangesAsync();
        var saved = await db.Activities.AsNoTracking().Ready().FirstAsync(a => a.Id == activity.Id);
        return Ok(Result(saved));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertActivityRequest request)
    {
        var me = RequireUser();
        var activity = await db.Activities.Include(a => a.Registrations).FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new ApiException(404, "找不到这场活动");
        if (activity.OrganizerId != me.Id)
            throw new ApiException(403, "只能修改自己发布的活动");
        var paid = activity.Registrations.Any(r => r.Status == RegistrationStatus.Paid);
        if (activity.Status == ActivityStatus.Published && paid)
            throw new ApiException(400, "已经有人付款，这场不能再改");
        await Apply(activity, request, me, creating: false);
        await db.SaveChangesAsync();
        activity = await db.Activities.AsNoTracking().Ready().FirstAsync(a => a.Id == id);
        return Ok(Result(activity));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var me = RequireUser();
        var activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new ApiException(404, "找不到这场活动");
        if (activity.OrganizerId != me.Id)
            throw new ApiException(403, "只能删除自己的草稿");
        if (activity.Status != ActivityStatus.Draft)
            throw new ApiException(400, "已发布的活动不能删除");
        db.Activities.Remove(activity);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id) => Ok(await booking.Join(id));

    async Task Apply(Activity activity, UpsertActivityRequest request, AppUser me, bool creating)
    {
        var club = await db.Clubs.FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new ApiException(404, "找不到俱乐部");
        if (club.OwnerId != me.Id)
            throw new ApiException(403, "只能用自己的俱乐部发布");
        var venue = await db.Venues.FirstOrDefaultAsync(v => v.Id == request.VenueId)
            ?? throw new ApiException(404, "找不到场馆");
        var sports = venue.Sports.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (!sports.Contains(club.Sport.ToString()))
            throw new ApiException(400, "这家场馆不办这项运动");
        if (!TimeOnly.TryParse(request.StartTime, CultureInfo.InvariantCulture, out var start) ||
            !TimeOnly.TryParse(request.EndTime, CultureInfo.InvariantCulture, out var end))
            throw new ApiException(400, "时间格式要用 18:00 这样");
        if (end <= start)
            throw new ApiException(400, "结束时间要晚于开始时间");
        var startAt = AucklandTime.At(request.Date, start);
        if (startAt <= DateTimeOffset.UtcNow)
            throw new ApiException(400, "开始时间要在现在之后");
        if (request.Capacity is < 2 or > 80)
            throw new ApiException(400, "人数上限要在 2 到 80 之间");
        if (request.Price is < 0 or > 500)
            throw new ApiException(400, "费用看起来不对");
        if (request.WomenDiscount < 0 || request.WomenDiscount > request.Price)
            throw new ApiException(400, "女士优惠不能比报名费还高");
        if (!Fees.HalfStep(request.SkillMin) || !Fees.HalfStep(request.SkillMax) || request.SkillMin > request.SkillMax)
            throw new ApiException(400, "水平范围要用 L1 到 L8，步长 0.5");
        if (request.RefundPolicy is not ("h24" or "h12" or "none"))
            throw new ApiException(400, "退款规则不正确");
        if (request.CourtsUsed is < 1 or > 30)
            throw new ApiException(400, "场地片数不对");
        if (request.PlayersPerCourt is < 1 or > 20)
            throw new ApiException(400, "每片人数不对");

        var wasPublished = activity.Status == ActivityStatus.Published;
        activity.ClubId = club.Id;
        activity.VenueId = venue.Id;
        activity.OrganizerId = me.Id;
        activity.Sport = club.Sport;
        activity.Title = Required(request.Title, "标题", 60);
        activity.TitleEn = Optional(request.TitleEn, 80) ?? "";
        activity.StartAt = startAt;
        activity.EndAt = AucklandTime.At(request.Date, end);
        activity.Capacity = request.Capacity;
        activity.Price = request.Price;
        activity.WomenDiscount = request.WomenDiscount;
        activity.SkillMin = request.SkillMin;
        activity.SkillMax = request.SkillMax;
        activity.CourtsBooked = request.CourtsBooked;
        activity.CourtsUsed = request.CourtsUsed;
        activity.PlayersPerCourt = request.PlayersPerCourt;
        activity.CourtNumbers = Optional(request.CourtNumbers, 40);
        activity.Shuttlecock = Optional(request.Shuttlecock, 60);
        activity.RefundPolicy = request.RefundPolicy;
        activity.ParkingFree = request.ParkingFree;
        activity.WomenFriendly = request.WomenFriendly;
        activity.WelcomeNewcomers = request.WelcomeNewcomers;
        activity.Competitive = request.Competitive;
        activity.Casual = request.Casual;
        activity.Status = request.Publish ? ActivityStatus.Published : ActivityStatus.Draft;
        if (request.Publish && !wasPublished)
            club.EventsHosted += 1;
        if (creating)
            activity.CreatedAt = Now;
    }

    SaveActivityResult Result(Activity activity)
    {
        var estimate = Fees.Estimate(activity.Price, activity.Capacity, FeePercent);
        return new SaveActivityResult(
            ActivityMap.ToDto(activity, Me?.Id, Now),
            $"{Frontend}/activities/{activity.Id}",
            estimate.Gross,
            estimate.Fee,
            estimate.Net);
    }

    AppUser RequireUser() => Me ?? throw new ApiException(401, "请先选择一个演示账号");

    static string Required(string? value, string label, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length < 2)
            throw new ApiException(400, $"{label}至少 2 个字");
        if (text.Length > max)
            throw new ApiException(400, $"{label}太长了");
        return text;
    }

    static string? Optional(string? value, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length == 0)
            return null;
        if (text.Length > max)
            throw new ApiException(400, "有一项文字太长了");
        return text;
    }
}

[ApiController]
[Route("api")]
public class RegistrationsController(BookingService booking) : ControllerBase
{
    [HttpPost("registrations/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await booking.Cancel(id);
        return NoContent();
    }

    [HttpPost("registrations/{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(Guid id) => Ok(await booking.Transfer(id));

    [HttpPost("transfers/{code}/claim")]
    public async Task<IActionResult> Claim(string code)
    {
        await booking.Claim(code);
        return NoContent();
    }
}
