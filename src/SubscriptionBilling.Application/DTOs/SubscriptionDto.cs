namespace SubscriptionBilling.Application.DTOs;

public sealed record SubscriptionDto(
    Guid Id,
    Guid CustomerId,
    string PlanCode,
    decimal Amount,
    string Currency,
    string BillingCycle,
    string Status,
    DateTime StartDateUtc,
    DateTime? NextBillingDateUtc,
    DateTime? CancelledAtUtc);
