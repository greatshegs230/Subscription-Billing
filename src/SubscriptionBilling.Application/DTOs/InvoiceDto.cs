namespace SubscriptionBilling.Application.DTOs;

public sealed record InvoiceDto(
    Guid Id,
    Guid SubscriptionId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    DateTime DueDateUtc,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc);
