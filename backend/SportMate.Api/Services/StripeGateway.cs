using Stripe;
using Stripe.Checkout;
using SportMate.Api.Infrastructure;
using SportMate.Api.Models;

namespace SportMate.Api.Services;

public class StripeGateway(IConfiguration config, ILogger<StripeGateway> logger)
{
    public bool Enabled
    {
        get
        {
            var key = config["Stripe:SecretKey"];
            return !string.IsNullOrWhiteSpace(key) && key.StartsWith("sk_", StringComparison.Ordinal);
        }
    }

    public string? PublishableKey
    {
        get
        {
            var key = config["Stripe:PublishableKey"];
            return string.IsNullOrWhiteSpace(key) ? null : key;
        }
    }

    string Frontend => (config["FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');

    void UseKey() => StripeConfiguration.ApiKey = config["Stripe:SecretKey"];

    public async Task<(string Id, string Url)> CreateCheckout(Registration registration, Activity activity, AppUser user, Club club, decimal feePercent)
    {
        UseKey();
        var amountCents = ToCents(registration.Amount);
        var feeCents = (long)Math.Round(amountCents * feePercent / 100m, 0, MidpointRounding.AwayFromZero);
        var destination = club.StripeAccountId;
        var canTransfer = !string.IsNullOrWhiteSpace(destination) && destination.StartsWith("acct_", StringComparison.Ordinal);

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = user.Email,
            ClientReferenceId = registration.Id.ToString(),
            SuccessUrl = $"{Frontend}/activities/{activity.Id}?paid=1&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{Frontend}/activities/{activity.Id}?paid=0&registration={registration.Id}",
            Metadata = new Dictionary<string, string> { ["registrationId"] = registration.Id.ToString() },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "nzd",
                        UnitAmount = amountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = activity.Title,
                            Description = "SportMate session registration"
                        }
                    }
                }
            ]
        };

        if (canTransfer)
        {
            options.PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = feeCents,
                TransferData = new SessionPaymentIntentDataTransferDataOptions { Destination = destination }
            };
        }

        try
        {
            var session = await new SessionService().CreateAsync(options);
            return (session.Id, session.Url);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe checkout failed");
            throw new ApiException(502, "Stripe 暂时没有完成这笔操作：" + (ex.StripeError?.Message ?? ex.Message));
        }
    }

    public async Task<Session> Retrieve(string sessionId)
    {
        UseKey();
        try { return await new SessionService().GetAsync(sessionId); }
        catch (StripeException ex)
        {
            throw new ApiException(502, "Stripe 暂时没有完成这笔操作：" + (ex.StripeError?.Message ?? ex.Message));
        }
    }

    public async Task Refund(string paymentIntentId)
    {
        UseKey();
        try
        {
            await new RefundService().CreateAsync(new RefundCreateOptions { PaymentIntent = paymentIntentId });
        }
        catch (StripeException ex)
        {
            throw new ApiException(502, "Stripe 退款没有完成：" + (ex.StripeError?.Message ?? ex.Message));
        }
    }

    public Event ConstructEvent(string json, string signature)
    {
        var secret = config["Stripe:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new ApiException(503, "还没有配置 Stripe webhook 密钥");
        try { return EventUtility.ConstructEvent(json, signature, secret); }
        catch (StripeException)
        {
            throw new ApiException(400, "Stripe 签名校验没有通过");
        }
    }

    public async Task<(string AccountId, string Url)> StartConnect(Club club, string email)
    {
        UseKey();
        try
        {
            var accountId = club.StripeAccountId;
            if (string.IsNullOrWhiteSpace(accountId) || accountId == "demo")
            {
                var account = await new AccountService().CreateAsync(new AccountCreateOptions
                {
                    Type = "express",
                    Country = "NZ",
                    Email = email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                    },
                    BusinessProfile = new AccountBusinessProfileOptions { Name = club.Name }
                });
                accountId = account.Id;
            }

            var link = await new AccountLinkService().CreateAsync(new AccountLinkCreateOptions
            {
                Account = accountId,
                Type = "account_onboarding",
                RefreshUrl = $"{Frontend}/clubs/{club.Id}?stripe=refresh",
                ReturnUrl = $"{Frontend}/clubs/{club.Id}?stripe=return"
            });
            return (accountId, link.Url);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe connect failed");
            throw new ApiException(502, "Stripe 开户没有完成：" + (ex.StripeError?.Message ?? ex.Message));
        }
    }

    public async Task<bool> ChargesEnabled(string accountId)
    {
        UseKey();
        try
        {
            var account = await new AccountService().GetAsync(accountId);
            return account.ChargesEnabled;
        }
        catch (StripeException ex)
        {
            throw new ApiException(502, "Stripe 暂时没有返回账户状态：" + (ex.StripeError?.Message ?? ex.Message));
        }
    }

    static long ToCents(decimal amount) =>
        (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
}
