using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.DTOs;

namespace SubscriptionBilling.Application.Invoices.Queries;

public sealed class GetInvoicesByCustomerQueryHandler : IQueryHandler<GetInvoicesByCustomerQuery, IReadOnlyList<InvoiceDto>>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public GetInvoicesByCustomerQueryHandler(
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository)
    {
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<IReadOnlyList<InvoiceDto>> HandleAsync(GetInvoicesByCustomerQuery query, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(query.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new KeyNotFoundException("Customer was not found.");
        }

        var invoices = await _invoiceRepository.GetByCustomerIdAsync(query.CustomerId, cancellationToken);

        return invoices
            .Select(x => new InvoiceDto(
                x.Id,
                x.SubscriptionId,
                x.CustomerId,
                x.Amount.Value,
                x.Amount.Currency,
                x.DueDateUtc,
                x.Status.ToString(),
                x.CreatedAtUtc,
                x.PaidAtUtc))
            .ToList();
    }
}
