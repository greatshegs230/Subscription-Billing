namespace SubscriptionBilling.Application.Outbox;

public sealed record OutboxMessageDto(
    Guid Id,
    string Type,
    string Payload,
    DateTime OccurredOnUtc,
    DateTime? ProcessedOnUtc,
    DateTime? LastAttemptedOnUtc,
    int AttemptCount,
    string? Error);
