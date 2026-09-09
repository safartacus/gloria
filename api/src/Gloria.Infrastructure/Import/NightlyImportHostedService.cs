using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gloria.Infrastructure.Import;

/// <summary>
/// Scheduler for the nightly end-of-day batch. At the configured local time it runs
/// <see cref="NightlyBatchRunner"/> (roster sync + folder import + commission recalculation) in a
/// fresh scope. The same runner backs the manual "run now" endpoint. Disabled unless
/// <c>Gloria:NightlyImport:Enabled = true</c>.
/// </summary>
public sealed class NightlyImportHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NightlyImportOptions _options;
    private readonly ILogger<NightlyImportHostedService> _logger;

    public NightlyImportHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<NightlyImportOptions> options,
        ILogger<NightlyImportHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Nightly batch disabled (Gloria:NightlyImport:Enabled = false). Manual 'run now' endpoint still works.");
            return;
        }

        var runAt = TimeOnly.TryParse(_options.RunAtLocalTime, out var t) ? t : new TimeOnly(2, 0);
        _logger.LogInformation("Nightly batch enabled. Daily at {RunAt}.", runAt);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var next = now.Date + runAt.ToTimeSpan();
            if (next <= now) next = next.AddDays(1);

            _logger.LogInformation("Next nightly batch at {Next:o}.", next);
            try
            {
                await Task.Delay(next - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<NightlyBatchRunner>();
                await runner.RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nightly batch run failed.");
            }
        }
    }
}
