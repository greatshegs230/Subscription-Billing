using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Events;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Subscriptions;

public sealed class Subscription : AggregateRoot
{
    private Subscription()
    {
    }

    private Subscription(
        Guid id,
        Guid customerId,
        string planCode,
        Money amount,
        BillingCycle billingCycle,
        DateTime startDateUtc)
    {
        Id = id;
        CustomerId = customerId;
        PlanCode = planCode;
        Amount = amount;
        BillingCycle = billingCycle;
        StartDateUtc = startDateUtc.Date;
        Status = SubscriptionStatus.Draft;
    }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public Money Amount { get; private set; } = null!;
    public BillingCycle BillingCycle { get; private set; } = null!;
    public SubscriptionStatus Status { get; private set; }
    public DateTime StartDateUtc { get; private set; }
    public DateTime? NextBillingDateUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public static (Subscription Subscription, Invoice FirstInvoice) CreateAndActivate(
        Guid customerId,
        string planCode,
        Money amount,
        BillingCycle billingCycle,
        DateTime startDateUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer id is required.");
        }

        if (string.IsNullOrWhiteSpace(planCode))
        {
            throw new DomainException("Plan code is required.");
        }

        var subscription = new Subscription(
            Guid.NewGuid(),
            customerId,
            planCode.Trim().ToUpperInvariant(),
            amount,
            billingCycle,
            startDateUtc);

        var firstInvoice = subscription.Activate();

        return (subscription, firstInvoice);
    }

    public Invoice Activate()
    {
        if (Status == SubscriptionStatus.Active)
        {
            throw new DomainException("Subscription is already active.");
        }

        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException("Cancelled subscription cannot be activated.");
        }

        Status = SubscriptionStatus.Active;
        NextBillingDateUtc = BillingCycle.CalculateNextBillingDate(StartDateUtc);

        Raise(SubscriptionActivated.Create(Id, CustomerId));

        return Invoice.Create(Id, CustomerId, Amount, StartDateUtc);
    }

    public void Cancel(DateTime cancelledAtUtc)
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException("Subscription is already cancelled.");
        }

        Status = SubscriptionStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
        NextBillingDateUtc = null;
    }

    public Invoice? GenerateNextInvoiceIfDue(DateTime asOfUtc)
    {
        if (Status != SubscriptionStatus.Active)
        {
            return null;
        }

        if (!NextBillingDateUtc.HasValue)
        {
            throw new DomainException("Active subscription must have a next billing date.");
        }

        if (asOfUtc.Date < NextBillingDateUtc.Value.Date)
        {
            return null;
        }

        var invoice = Invoice.Create(Id, CustomerId, Amount, NextBillingDateUtc.Value.Date);
        NextBillingDateUtc = BillingCycle.CalculateNextBillingDate(NextBillingDateUtc.Value.Date);

        return invoice;
    }
}
