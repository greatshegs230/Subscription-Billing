using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed record CancelSubscriptionCommand(Guid SubscriptionId) : ICommand<bool>;
