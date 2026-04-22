namespace SubscriptionBilling.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
    public DateTime? LastAttemptedOnUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? Error { get; set; }
}
