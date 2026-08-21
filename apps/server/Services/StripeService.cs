namespace server.Services;

using Stripe;
using server.Data.Models;

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly CustomerService _customerService;
    private readonly SetupIntentService _setupIntentService;
    private readonly PaymentIntentService _paymentIntentService;

    public StripeService(IConfiguration config)
    {
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"] ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        _customerService = new CustomerService();
        _setupIntentService = new SetupIntentService();
        _paymentIntentService = new PaymentIntentService();
    }

    public async Task<string> GetOrCreateCustomerAsync(User user)
    {
        if (!string.IsNullOrEmpty(user.StripeCustomerId))
        {
            return user.StripeCustomerId;
        }

        var options = new CustomerCreateOptions
        {
            Email = user.Email,
            Name = $"{user.FirstName} {user.LastName}".Trim(),
            Metadata = new Dictionary<string, string> { { "UserId", user.Id } }
        };

        var customer = await _customerService.CreateAsync(options);
        return customer.Id;
    }

    public async Task<SetupIntent> CreateSetupIntentAsync(string customerId)
    {
        var options = new SetupIntentCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Usage = "off_session"
        };

        return await _setupIntentService.CreateAsync(options);
    }

    public async Task<PaymentIntent> ChargeCustomerAsync(string customerId, int amountCents, string description = "Penalty")
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = "usd",
            Customer = customerId,
            Confirm = true,
            OffSession = true,
            Description = description
        };

        return await _paymentIntentService.CreateAsync(options);
    }
}
