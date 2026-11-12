using Microsoft.EntityFrameworkCore;
using SportMate.Api.Contracts;
using SportMate.Api.Data;
using SportMate.Api.Infrastructure;
using SportMate.Api.Mapping;
using SportMate.Api.Models;
using Stripe.Checkout;

namespace SportMate.Api.Services;

public class BookingService(AppDbContext db, CurrentUserAccessor current, StripeGateway stripe, IConfiguration config)
{
    AppUser Me() => current.User ?? throw new ApiException(401, "请先选择一个演示账号");

    decimal FeePercent => config.GetValue("PlatformFeePercent", 8m);

    string Frontend => (config["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

    public async Task<JoinResult> Join(Guid activityId)
    {
        var me = Me();
        var now = DateTimeOffset.UtcNow;
        await using var tx = await db.Database.BeginTransactionAsync();
        var activity = await db.Activities
            .Include(a => a.Club)
            .Include(a => a.Registrations)
            .FirstOrDefaultAsync(a => a.Id == activityId)
            ?? throw new ApiException(404, "找不到这场活动");

        if (activity.Status != ActivityStatus.Published)
            throw new ApiException(400, "这场活动还不能报名");
        if (activity.StartAt <= now)
            throw new ApiException(400, "活动已经开始，不能再报名");
        if (me.SkillLevel < activity.SkillMin || me.SkillLevel > activity.SkillMax)
            throw new ApiException(400, $"这场水平是 L{Fmt(activity.SkillMin)}–L{Fmt(activity.SkillMax)}，你现在是 L{Fmt(me.SkillLevel)}。可以在「我的」里调整演示水平。");

        foreach (var stale in activity.Registrations.Where(r => r.Status == RegistrationStatus.PendingPayment && r.CreatedAt < now.AddMinutes(-15)))
            stale.Status = RegistrationStatus.Cancelled;

        var existing = activity.Registrations.FirstOrDefault(r =>
            r.UserId == me.Id && r.Status is RegistrationStatus.Paid or RegistrationStatus.PendingPayment or RegistrationStatus.Waitlist);
        if (existing?.Status == RegistrationStatus.Paid)
            throw new ApiException(400, "你已经报名了");
        if (existing?.Status == RegistrationStatus.Waitlist)
            throw new ApiException(400, "你已经在候补名单里");

        var alreadyHolding = existing?.Status == RegistrationStatus.PendingPayment;
        if (!alreadyHolding && ActivityMap.Holding(activity, now) >= activity.Capacity)
        {
            var position = activity.Registrations.Count(r => r.Status == RegistrationStatus.Waitlist) + 1;
            var wait = new Registration
            {
                Id = Guid.NewGuid(),
                ActivityId = activity.Id,
                UserId = me.Id,
                Status = RegistrationStatus.Waitlist,
                Amount = 0,
                WaitlistPosition = position,
                CreatedAt = now
            };
            db.Registrations.Add(wait);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new JoinResult(wait.Id, "Waitlist", 0, position, "none", null);
        }

        var amount = Fees.Charge(activity.Price, activity.WomenDiscount, me.Gender);
        var registration = existing ?? new Registration
        {
            Id = Guid.NewGuid(),
            ActivityId = activity.Id,
            UserId = me.Id
        };
        registration.Status = RegistrationStatus.PendingPayment;
        registration.Amount = amount;
        registration.CreatedAt = now;
        if (existing == null)
            db.Registrations.Add(registration);
        await db.SaveChangesAsync();

        string? url = null;
        var mode = "demo";
        if (stripe.Enabled)
        {
            var checkout = await stripe.CreateCheckout(registration, activity, me, activity.Club, FeePercent);
            registration.CheckoutSessionId = checkout.Id;
            url = checkout.Url;
            mode = "stripe";
            await db.SaveChangesAsync();
        }

        await tx.CommitAsync();
        return new JoinResult(registration.Id, registration.Status.ToString(), amount, null, mode, url);
    }

    public async Task CompleteDemo(Guid registrationId)
    {
        if (stripe.Enabled)
            throw new ApiException(400, "已经配置了 Stripe，请走正式支付");
        var me = Me();
        var registration = await LoadMine(registrationId, me.Id);
        if (registration.Status == RegistrationStatus.Paid)
            return;
        if (registration.Status != RegistrationStatus.PendingPayment)
            throw new ApiException(400, "这笔报名不能确认支付");
        MarkPaid(registration, PaymentProvider.Demo, null, null);
        await db.SaveChangesAsync();
    }

    public async Task ConfirmStripe(string sessionId)
    {
        var me = Me();
        if (!stripe.Enabled)
            throw new ApiException(400, "还没有配置 Stripe");
        var session = await stripe.Retrieve(sessionId);
        if (session.PaymentStatus != "paid")
            throw new ApiException(400, "Stripe 还没有收到这笔付款");
        if (!session.Metadata.TryGetValue("registrationId", out var raw) || !Guid.TryParse(raw, out var registrationId))
            throw new ApiException(400, "这笔支付对不上报名");
        var registration = await LoadMine(registrationId, me.Id);
        if (registration.Status == RegistrationStatus.Paid)
            return;
        if (registration.Status != RegistrationStatus.PendingPayment)
            throw new ApiException(400, "报名已取消，请重新报名");
        MarkPaid(registration, PaymentProvider.Stripe, session.Id, session.PaymentIntentId);
        await db.SaveChangesAsync();
    }

    public async Task HandleWebhook(string json, string signature)
    {
        var stripeEvent = stripe.ConstructEvent(json, signature);
        if (stripeEvent.Type == "checkout.session.completed" && stripeEvent.Data.Object is Session session && session.PaymentStatus == "paid")
        {
            if (session.Metadata.TryGetValue("registrationId", out var raw) && Guid.TryParse(raw, out var registrationId))
            {
                var registration = await db.Registrations.FirstOrDefaultAsync(r => r.Id == registrationId);
                if (registration?.Status == RegistrationStatus.PendingPayment)
                {
                    MarkPaid(registration, PaymentProvider.Stripe, session.Id, session.PaymentIntentId);
                    await db.SaveChangesAsync();
                }
            }
        }
        else if (stripeEvent.Type == "checkout.session.expired" && stripeEvent.Data.Object is Session expired &&
                 expired.Metadata.TryGetValue("registrationId", out var rawId) && Guid.TryParse(rawId, out var id))
        {
            var registration = await db.Registrations.FirstOrDefaultAsync(r => r.Id == id);
            if (registration?.Status == RegistrationStatus.PendingPayment)
            {
                registration.Status = RegistrationStatus.Cancelled;
                await db.SaveChangesAsync();
            }
        }
    }

    public async Task Cancel(Guid registrationId)
    {
        var me = Me();
        var registration = await db.Registrations
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == me.Id)
            ?? throw new ApiException(404, "找不到这笔报名");

        if (registration.Status is RegistrationStatus.Cancelled or RegistrationStatus.Transferred)
            return;
        if (registration.Activity.StartAt <= DateTimeOffset.UtcNow)
            throw new ApiException(400, "活动已经开始");
        if (registration.Status == RegistrationStatus.Paid && RefundLocked(registration.Activity, DateTimeOffset.UtcNow))
            throw new ApiException(400, RefundMessage(registration.Activity.RefundPolicy));

        if (registration.Status == RegistrationStatus.Paid)
        {
            var payment = await db.Payments.FirstOrDefaultAsync(p => p.RegistrationId == registration.Id && p.Status == "succeeded");
            if (payment?.Provider == PaymentProvider.Stripe && !string.IsNullOrEmpty(payment.PaymentIntentId))
                await stripe.Refund(payment.PaymentIntentId);
            if (payment != null)
                payment.Status = "refunded";
        }

        registration.Status = RegistrationStatus.Cancelled;
        registration.TransferCode = null;
        await db.SaveChangesAsync();
    }

    public async Task<TransferResult> Transfer(Guid registrationId)
    {
        var me = Me();
        var registration = await db.Registrations
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == me.Id)
            ?? throw new ApiException(404, "找不到这笔报名");
        if (registration.Status != RegistrationStatus.Paid)
            throw new ApiException(400, "只有已支付的名额可以转让");
        if (registration.Activity.StartAt <= DateTimeOffset.UtcNow)
            throw new ApiException(400, "活动已经开始");
        registration.TransferCode = Convert.ToHexString(Guid.NewGuid().ToByteArray())[..8].ToLowerInvariant();
        await db.SaveChangesAsync();
        return new TransferResult(registration.TransferCode, $"{Frontend}/activities/{registration.ActivityId}?transfer={registration.TransferCode}");
    }

    public async Task Claim(string code)
    {
        var me = Me();
        var now = DateTimeOffset.UtcNow;
        var registration = await db.Registrations
            .Include(r => r.Activity)
            .ThenInclude(a => a.Registrations)
            .FirstOrDefaultAsync(r => r.TransferCode == code)
            ?? throw new ApiException(404, "转让链接失效了");
        var activity = registration.Activity;
        if (registration.Status != RegistrationStatus.Paid)
            throw new ApiException(400, "这个名额已经不能转让");
        if (activity.StartAt <= now)
            throw new ApiException(400, "活动已经开始");
        if (registration.UserId == me.Id)
            throw new ApiException(400, "这是你自己的名额");
        if (me.SkillLevel < activity.SkillMin || me.SkillLevel > activity.SkillMax)
            throw new ApiException(400, $"这场水平是 L{Fmt(activity.SkillMin)}–L{Fmt(activity.SkillMax)}，你现在是 L{Fmt(me.SkillLevel)}");
        if (activity.Registrations.Any(r => r.UserId == me.Id && r.Status is RegistrationStatus.Paid or RegistrationStatus.PendingPayment or RegistrationStatus.Waitlist))
            throw new ApiException(400, "你已经在这场的名单里");

        registration.Status = RegistrationStatus.Transferred;
        registration.TransferCode = null;
        db.Registrations.Add(new Registration
        {
            Id = Guid.NewGuid(),
            ActivityId = activity.Id,
            UserId = me.Id,
            Status = RegistrationStatus.Paid,
            Amount = registration.Amount,
            FromTransfer = true,
            CreatedAt = now
        });
        await db.SaveChangesAsync();
    }

    async Task<Registration> LoadMine(Guid id, Guid userId) =>
        await db.Registrations.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
        ?? throw new ApiException(404, "找不到这笔报名");

    void MarkPaid(Registration registration, PaymentProvider provider, string? sessionId, string? paymentIntentId)
    {
        registration.Status = RegistrationStatus.Paid;
        if (!string.IsNullOrEmpty(sessionId))
            registration.CheckoutSessionId = sessionId;
        db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            RegistrationId = registration.Id,
            Provider = provider,
            Status = "succeeded",
            AmountCents = (long)Math.Round(registration.Amount * 100m, 0, MidpointRounding.AwayFromZero),
            Currency = "nzd",
            CheckoutSessionId = sessionId,
            PaymentIntentId = paymentIntentId,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public static bool RefundLocked(Activity activity, DateTimeOffset now)
    {
        var hours = (activity.StartAt - now).TotalHours;
        return activity.RefundPolicy switch
        {
            "none" => true,
            "h12" => hours < 12,
            _ => hours < 24
        };
    }

    public static string RefundMessage(string policy) => policy switch
    {
        "none" => "这场不支持退款，可以把名额转让给别人",
        "h12" => "开始前 12 小时内不可退，可以把名额转让给别人",
        _ => "开始前 24 小时内不可退，可以把名额转让给别人"
    };

    static string Fmt(decimal value) => value == decimal.Truncate(value) ? value.ToString("0") : value.ToString("0.0");
}
