namespace SubscriptionBilling.Application.Idempotency;

public sealed record IdempotencyRecordDto(
    Guid Id,
    string Key,
    string CommandName,
    string RequestHash,
    string ResponseJson,
    DateTime CreatedAtUtc);
