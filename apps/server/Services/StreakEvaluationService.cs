namespace server.Services;

using Microsoft.EntityFrameworkCore;
using server.Data;
using server.Data.Models;
using Microsoft.Extensions.Logging;

public interface IStreakEvaluationService
{
    Task EvaluateDailyStreaksAsync(DateTime dateToEvaluate, CancellationToken cancellationToken = default);
}

public class StreakEvaluationService(
    ApplicationDbContext db,
    IStripeService stripeService,
    CentrifugoService centrifugoService,
    ILogger<StreakEvaluationService> logger) : IStreakEvaluationService
{                            
    private readonly ApplicationDbContext _db = db;
    private readonly IStripeService _stripeService = stripeService;
    private readonly CentrifugoService _centrifugoService = centrifugoService;
    private readonly ILogger<StreakEvaluationService> _logger = logger;

    public async Task EvaluateDailyStreaksAsync(DateTime dateToEvaluate, CancellationToken cancellationToken = default)
    {
        var targetDate = dateToEvaluate.Date;
        _logger.LogInformation("Starting daily streak evaluation for {Date}", targetDate.ToShortDateString());

        var activeGroups = await _db.Groups
            .Where(g => g.StreakCount > 0)
            .ToListAsync(cancellationToken);

        foreach (var group in activeGroups)
        {
            var usersInGroup = await _db.GroupUsers
                .Include(gu => gu.User)
                .Where(gu => gu.GroupId == group.Id)
                .Select(gu => gu.User)
                .ToListAsync(cancellationToken);

            // requires 2 users to evaluate a shared streak
            if (usersInGroup.Count < 2) continue;

            // bunch of slackers!
            var slackers = new List<User>();
            var allPosted = true;

            foreach (var user in usersInGroup)
            {
                var posted = await _db.Entries
                    .AnyAsync(e => e.GroupId == group.Id 
                                   && e.AuthorId == user.Id 
                                   && e.CreatedAt.Date == targetDate, cancellationToken);

                if (!posted)
                {
                    allPosted = false;
                    slackers.Add(user);
                }
            }

            if (allPosted)
            {
                group.StreakCount++;
                _logger.LogInformation("Group {GroupId} streak incremented to {StreakCount}.", group.Id, group.StreakCount);
            }
            else
            {
                group.StreakCount = 0;
                _logger.LogInformation("Group {GroupId} streak broken by {SlackerCount} user(s).", group.Id, slackers.Count);

                foreach (var slacker in slackers)
                {
                    if (slacker.IsPenaltyEnabled && !string.IsNullOrEmpty(slacker.StripeCustomerId) && slacker.PenaltyAmount > 0)
                    {
                        try
                        {
                            await _stripeService.ChargeCustomerAsync(
                                slacker.StripeCustomerId, 
                                slacker.PenaltyAmount, 
                                $"Penalty for broken streak in group {group.Id}");
                                
                            _logger.LogInformation("Charged {Email} penalty of {Amount} cents.", slacker.Email, slacker.PenaltyAmount);
                            
                            var notificationPayload = new { type = "penalty_charged", amount = slacker.PenaltyAmount };
                            await _centrifugoService.PublishAsync($"user#{slacker.Id}", notificationPayload);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to charge {Email}.", slacker.Email);
                        }
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Daily streak evaluation completed.");
    }
}
