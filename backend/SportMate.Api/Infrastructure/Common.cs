using System.Text.Json;

namespace SportMate.Api.Infrastructure;

public class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

public class CurrentUserAccessor
{
    public Models.AppUser? User { get; set; }
}

public static class AucklandTime
{
    static readonly TimeZoneInfo Zone = Load();

    static TimeZoneInfo Load()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Pacific/Auckland"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("New Zealand Standard Time"); }
    }

    public static DateTimeOffset Now() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, Zone);

    public static DateOnly Today() => DateOnly.FromDateTime(Now().DateTime);

    public static DateOnly DateOf(DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);

    public static DateOnly Next(DateOnly from, DayOfWeek day)
    {
        var delta = ((int)day - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(delta);
    }

    public static DateTimeOffset At(DateOnly date, TimeOnly time)
    {
        var wall = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(wall, Zone);
        return new DateTimeOffset(utc);
    }
}

public static class Fees
{
    public static decimal Charge(decimal price, decimal womenDiscount, Models.Gender gender) =>
        gender == Models.Gender.Female ? Math.Max(0, price - womenDiscount) : price;

    public static (decimal Gross, decimal Fee, decimal Net) Estimate(decimal price, int capacity, decimal percent)
    {
        var gross = price * capacity;
        var fee = Math.Round(gross * percent / 100m, 2, MidpointRounding.AwayFromZero);
        return (gross, fee, gross - fee);
    }

    public static bool HalfStep(decimal skill) =>
        skill is >= 1 and <= 8 && skill * 2 == decimal.Truncate(skill * 2);
}

public class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.Status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "服务器开了个小差，请稍后再试" }));
        }
    }
}

public class CurrentUserMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, Data.AppDbContext db, CurrentUserAccessor current)
    {
        if (context.Request.Headers.TryGetValue("X-User-Id", out var values) && Guid.TryParse(values.ToString(), out var id))
            current.User = await db.Users.FindAsync(id);
        await next(context);
    }
}
