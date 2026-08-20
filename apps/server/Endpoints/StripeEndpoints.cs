namespace server.Endpoints;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using server.Data;
using server.Services;
using System.Security.Claims;
using NSwag.Annotations;

public static class StripeEndpoints
{
    public static void RegisterStripeEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/stripe").WithTags("Stripe");

        group.MapPost("/setup-intent", CreateSetupIntent)
            .RequireAuthorization()
            .WithSummary("Create a Stripe SetupIntent for the current user");

        group.MapPost("/webhook", StripeWebhook)
            .AllowAnonymous()
            .WithSummary("Stripe Webhook listener");
    }

    public class SetupIntentResponse
    {
        public string ClientSecret { get; set; } = string.Empty;
    }

    [OpenApiOperation("CreateSetupIntent", "Creates a SetupIntent to save a card")]
    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<SetupIntentResponse>, Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult, Microsoft.AspNetCore.Http.HttpResults.NotFound>> CreateSetupIntent(
        ApplicationDbContext db,
        StripeService stripeService,
        ClaimsPrincipal userClaims)
    {
        var userId = userClaims.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return TypedResults.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user == null) return TypedResults.NotFound();

        var customerId = await stripeService.GetOrCreateCustomerAsync(user);
        
        // Save customer ID if we just created it
        if (user.StripeCustomerId != customerId)
        {
            user.StripeCustomerId = customerId;
            await db.SaveChangesAsync();
        }

        var setupIntent = await stripeService.CreateSetupIntentAsync(customerId);
        
        return TypedResults.Ok(new SetupIntentResponse { ClientSecret = setupIntent.ClientSecret });
    }

    private static async Task<IResult> StripeWebhook(
        HttpRequest request,
        IConfiguration config,
        ApplicationDbContext db)
    {
        var json = await new StreamReader(request.Body).ReadToEndAsync();
        var endpointSecret = config["Stripe:WebhookSecret"] ?? Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET");

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                request.Headers["Stripe-Signature"],
                endpointSecret
            );

            // Handle the event
            if (stripeEvent.Type == EventTypes.SetupIntentSucceeded)
            {
                var setupIntent = stripeEvent.Data.Object as SetupIntent;
                if (setupIntent?.Customer != null)
                {
                    // Find user by customer ID
                    var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == setupIntent.Customer.Id);
                    if (user != null)
                    {
                        // Successfully added a card, could enable penalty here if we wanted, 
                        // but SettingsEndpoint handles that explicitly.
                        // For now we just log.
                        Console.WriteLine($"SetupIntentSucceeded for user {user.Id}");
                    }
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                Console.WriteLine($"PaymentIntentSucceeded for {paymentIntent?.Amount} cents.");
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                Console.WriteLine($"PaymentIntentFailed for {paymentIntent?.Amount} cents. Reason: {paymentIntent?.LastPaymentError?.Message}");
            }

            return Results.Ok();
        }
        catch (StripeException e)
        {
            Console.WriteLine($"Stripe Webhook Error: {e.Message}");
            return Results.BadRequest();
        }
    }
}
