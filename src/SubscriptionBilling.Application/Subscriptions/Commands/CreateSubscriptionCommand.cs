using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed record CreateSubscriptionCommand(
    Guid CustomerId,
    string PlanCode,
    string BillingCycle,
    DateTime StartDateUtc) : ICommand<Guid>;
