using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Customers.Commands;

public sealed record CreateCustomerCommand(string FullName, string Email) : ICommand<Guid>;
