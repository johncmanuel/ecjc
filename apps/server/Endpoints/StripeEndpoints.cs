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
        IStripeService stripeService,
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
        ApplicationDbContext db,
        CentrifugoService centrifugo)
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

            // Handle the event (we could do a switch statement, tbh, too lazy to fix rn)
            if (stripeEvent.Type == EventTypes.SetupIntentSucceeded)
            {
                var setupIntent = stripeEvent.Data.Object as SetupIntent;
                if (setupIntent?.Customer != null)
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == setupIntent.Customer.Id);
                    if (user != null)
                    {
                        Console.WriteLine($"SetupIntentSucceeded for user {user.Id}");
                    }
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == paymentIntent.CustomerId);
                if (user != null)
                {
                    var groupId = paymentIntent.Metadata?.GetValueOrDefault("GroupId");
                    var notificationPayload = new { type = "penalty_charged_success", amount = paymentIntent.Amount, groupId };
                    await centrifugo.PublishAsync($"user#{user.Id}", notificationPayload);
                    Console.WriteLine($"PaymentIntentSucceeded for {paymentIntent.Amount} cents for user {user.Id}.");
                }
            }
            else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == paymentIntent.CustomerId);
                if (user != null)
                {
                    user.IsPenaltyEnabled = false;
                    await db.SaveChangesAsync();
                    
                    var notificationPayload = new { type = "penalty_charged_failed", reason = "declined" };
                    await centrifugo.PublishAsync($"user#{user.Id}", notificationPayload);
                    Console.WriteLine($"PaymentIntentFailed for user {user.Id}. Disabled penalty.");
                }
            }
            else if (stripeEvent.Type == EventTypes.ChargeDisputeCreated)
            {
                var dispute = stripeEvent.Data.Object as Dispute;
                var chargeService = new ChargeService();
                var charge = await chargeService.GetAsync(dispute.ChargeId);
                
                var user = await db.Users.FirstOrDefaultAsync(u => u.StripeCustomerId == charge.CustomerId);
                if (user != null)
                {
                    user.IsPenaltyEnabled = false;
                    await db.SaveChangesAsync();
                    
                    var notificationPayload = new { type = "penalty_disputed" };
                    await centrifugo.PublishAsync($"user#{user.Id}", notificationPayload);
                    Console.WriteLine($"ChargeDisputeCreated for user {user.Id}. Disabled penalty.");
                }
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
