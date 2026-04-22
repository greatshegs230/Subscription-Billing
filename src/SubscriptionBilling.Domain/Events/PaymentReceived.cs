using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.Events;

public sealed record PaymentReceived(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid InvoiceId,
    Guid SubscriptionId,
    Guid CustomerId,
    decimal Amount,
    string Currency) : IDomainEvent
{
    public static PaymentReceived Create(Guid invoiceId, Guid subscriptionId, Guid customerId, decimal amount, string currency) =>
        new(Guid.NewGuid(), DateTime.UtcNow, invoiceId, subscriptionId, customerId, amount, currency);
}
