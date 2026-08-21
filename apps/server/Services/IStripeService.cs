namespace server.Services;

using Stripe;
using server.Data.Models;

public interface IStripeService
{
    Task<string> GetOrCreateCustomerAsync(User user);
    Task<SetupIntent> CreateSetupIntentAsync(string customerId);
    Task<PaymentIntent> ChargeCustomerAsync(string customerId, int amountCents, string description = "Penalty");
}
