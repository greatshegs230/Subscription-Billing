namespace SubscriptionBilling.Api.Contracts;

public sealed record CreateSubscriptionRequest(
    Guid CustomerId,
    string PlanCode,
    string BillingCycle,
    DateTime StartDateUtc);
