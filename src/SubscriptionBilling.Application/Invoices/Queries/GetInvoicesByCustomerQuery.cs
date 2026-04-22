using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.DTOs;

namespace SubscriptionBilling.Application.Invoices.Queries;

public sealed record GetInvoicesByCustomerQuery(Guid CustomerId) : IQuery<IReadOnlyList<InvoiceDto>>;
