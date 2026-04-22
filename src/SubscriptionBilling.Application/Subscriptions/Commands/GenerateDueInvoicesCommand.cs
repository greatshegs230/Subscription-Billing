using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed record GenerateDueInvoicesCommand(DateTime AsOfUtc) : ICommand<int>;
