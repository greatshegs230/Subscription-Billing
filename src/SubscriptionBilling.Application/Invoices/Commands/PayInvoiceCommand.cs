using SubscriptionBilling.Application.Abstractions;

namespace SubscriptionBilling.Application.Invoices.Commands;

public sealed record PayInvoiceCommand(Guid InvoiceId) : ICommand<bool>;
