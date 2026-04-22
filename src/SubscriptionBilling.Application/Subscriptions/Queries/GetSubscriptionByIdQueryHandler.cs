using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.DTOs;

namespace SubscriptionBilling.Application.Subscriptions.Queries;

public sealed class GetSubscriptionByIdQueryHandler : IQueryHandler<GetSubscriptionByIdQuery, SubscriptionDto>
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public GetSubscriptionByIdQueryHandler(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<SubscriptionDto> HandleAsync(GetSubscriptionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(query.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            throw new KeyNotFoundException("Subscription was not found.");
        }

        return new SubscriptionDto(
            subscription.Id,
            subscription.CustomerId,
            subscription.PlanCode,
            subscription.Amount.Value,
            subscription.Amount.Currency,
            subscription.BillingCycle.Value,
            subscription.Status.ToString(),
            subscription.StartDateUtc,
            subscription.NextBillingDateUtc,
            subscription.CancelledAtUtc);
    }
}
