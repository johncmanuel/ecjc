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
    ILogger<StreakEvaluationService> logger) : IStreakEvaluationService
{                            
    private readonly ApplicationDbContext _db = db;
    private readonly ILogger<StreakEvaluationService> _logger = logger;

    public async Task EvaluateDailyStreaksAsync(DateTime dateToEvaluate, CancellationToken cancellationToken = default)
    {
        var targetDate = dateToEvaluate.Date;
        var targetStart = new DateTimeOffset(DateTime.SpecifyKind(targetDate, DateTimeKind.Utc));
        var targetEnd = targetStart.AddDays(1);
        
        _logger.LogInformation("Starting daily streak evaluation for {Date}", targetDate.ToShortDateString());

        var activeGroups = await _db.Groups
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
                                   && e.CreatedAt >= targetStart 
                                   && e.CreatedAt < targetEnd, cancellationToken);

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
                    if (slacker.IsPenaltyEnabled && slacker.PenaltyAmount > 0)
                    {
                        slacker.AccumulatedPenaltyCents += slacker.PenaltyAmount;
                        _logger.LogInformation("Added penalty of {Amount} cents to {Email}. New total: {Total} cents.", 
                            slacker.PenaltyAmount, slacker.Email, slacker.AccumulatedPenaltyCents);
                    }
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Daily streak evaluation completed.");
    }
}
