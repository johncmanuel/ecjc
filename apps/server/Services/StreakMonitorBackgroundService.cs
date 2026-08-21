namespace server.Services;

public class StreakMonitorBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<StreakMonitorBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<StreakMonitorBackgroundService> _logger = logger;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StreakMonitorBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = _timeProvider.GetUtcNow();
            
            var tomorrow = now.UtcDateTime.Date.AddDays(1);
            var delay = tomorrow - now.UtcDateTime;

            _logger.LogInformation("Next streak evaluation scheduled in {TotalHours} hours.", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            var yesterday = _timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-1);

            _logger.LogInformation("Waking up to evaluate streaks for {Date}", yesterday.ToShortDateString());

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<IStreakEvaluationService>();
                await evaluationService.EvaluateDailyStreaksAsync(yesterday, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while evaluating daily streaks.");
            }
        }

        _logger.LogInformation("StreakMonitorBackgroundService is stopping.");
    }
}
