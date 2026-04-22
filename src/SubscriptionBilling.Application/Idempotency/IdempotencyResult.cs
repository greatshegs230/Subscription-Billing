namespace SubscriptionBilling.Application.Idempotency;

public sealed record IdempotencyResult<TResponse>(TResponse Response, bool IsReplay);
