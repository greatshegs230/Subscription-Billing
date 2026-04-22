using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed class CancelSubscriptionCommandHandler : ICommandHandler<CancelSubscriptionCommand, bool>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelSubscriptionCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(CancelSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(command.SubscriptionId, cancellationToken);
        if (subscription is null)
        {
            throw new KeyNotFoundException("Subscription was not found.");
        }

        subscription.Cancel(DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
