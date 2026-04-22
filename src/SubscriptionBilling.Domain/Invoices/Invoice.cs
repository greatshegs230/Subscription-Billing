using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Events;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Invoices;

public sealed class Invoice : AggregateRoot
{
    private Invoice()
    {
    }

    private Invoice(
        Guid id,
        Guid subscriptionId,
        Guid customerId,
        Money amount,
        DateTime dueDateUtc,
        DateTime createdAtUtc)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        CustomerId = customerId;
        Amount = amount;
        DueDateUtc = dueDateUtc.Date;
        CreatedAtUtc = createdAtUtc;
        Status = InvoiceStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public DateTime DueDateUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }

    public static Invoice Create(Guid subscriptionId, Guid customerId, Money amount, DateTime dueDateUtc)
    {
        var invoiceAmount = Money.Create(amount.Value, amount.Currency);
        var invoice = new Invoice(Guid.NewGuid(), subscriptionId, customerId, invoiceAmount, dueDateUtc, DateTime.UtcNow);
        invoice.Raise(InvoiceGenerated.Create(invoice.Id, subscriptionId, customerId, amount.Value, amount.Currency));
        return invoice;
    }

    public void Pay()
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new DomainException("Invoice has already been paid.");
        }

        Status = InvoiceStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;

        Raise(PaymentReceived.Create(Id, SubscriptionId, CustomerId, Amount.Value, Amount.Currency));
    }
}
