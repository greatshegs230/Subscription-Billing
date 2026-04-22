using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Subscriptions.Commands;

namespace SubscriptionBilling.Infrastructure.Services;

public sealed class BillingCycleBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BillingCycleBackgroundService> _logger;

    public BillingCycleBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BillingCycleBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<GenerateDueInvoicesCommandHandler>();
                var generated = await handler.HandleAsync(new GenerateDueInvoicesCommand(DateTime.UtcNow), stoppingToken);

                if (generated > 0)
                {
                    _logger.LogInformation("Generated {InvoiceCount} due invoice(s).", generated);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Billing cycle background worker failed.");
            }
        }
    }
}
