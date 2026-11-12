using System.Text.Json.Serialization;
using SportMate.Api.Data;
using SportMate.Api.Infrastructure;
using SportMate.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<StripeGateway>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddCors(options => options.AddPolicy("web", policy =>
{
    var frontend = builder.Configuration["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
    var origins = new[] { "http://localhost:5173", "http://127.0.0.1:5173", frontend }
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    policy.WithOrigins(origins)
        .AllowAnyHeader()
        .AllowAnyMethod();
}));

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
if (app.Environment.IsDevelopment())
    app.MapOpenApi();
app.UseCors("web");
app.UseMiddleware<CurrentUserMiddleware>();
app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { ok = true }));

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(db);
            break;
        }
        catch (Exception ex) when (attempt < 8 && IsDatabaseStarting(ex))
        {
            logger.LogWarning(ex, "数据库还没准备好，第 {Attempt} 次重试", attempt);
            await Task.Delay(1000);
        }
    }
}

app.Run();

static bool IsDatabaseStarting(Exception ex)
{
    for (Exception? current = ex; current != null; current = current.InnerException)
    {
        if (current is Npgsql.NpgsqlException)
            return true;
    }
    return false;
}
