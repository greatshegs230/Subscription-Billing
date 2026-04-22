using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;

namespace SubscriptionBilling.Application.Invoices.Commands;

public sealed class PayInvoiceCommandHandler : ICommandHandler<PayInvoiceCommand, bool>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PayInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(PayInvoiceCommand command, CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            throw new KeyNotFoundException("Invoice was not found.");
        }

        invoice.Pay();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
