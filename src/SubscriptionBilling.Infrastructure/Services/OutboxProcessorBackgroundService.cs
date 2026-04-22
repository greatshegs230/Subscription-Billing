using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionBilling.Application.Abstractions.Repositories;

namespace SubscriptionBilling.Infrastructure.Services;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }

                using var scope = _serviceProvider.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var messages = await outboxRepository.GetUnprocessedAsync(50, stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await outboxRepository.MarkAttemptStartedAsync(message.Id, stoppingToken);

                        _logger.LogInformation(
                            "Processing outbox message {MessageId} of type {Type}. Attempt {AttemptCount}.",
                            message.Id,
                            message.Type,
                            message.AttemptCount + 1);

                        await outboxRepository.MarkProcessedAsync(message.Id, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await outboxRepository.MarkFailedAsync(message.Id, ex.Message, stoppingToken);
                        _logger.LogError(ex, "Failed processing outbox message {MessageId}.", message.Id);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor failed.");
            }
        }
    }
}
