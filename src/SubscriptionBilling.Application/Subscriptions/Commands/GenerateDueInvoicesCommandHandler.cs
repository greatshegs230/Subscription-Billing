using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Invoices;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed class GenerateDueInvoicesCommandHandler : ICommandHandler<GenerateDueInvoicesCommand, int>
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GenerateDueInvoicesCommandHandler(
        ISubscriptionRepository subscriptionRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork)
    {
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> HandleAsync(GenerateDueInvoicesCommand command, CancellationToken cancellationToken = default)
    {
        var dueSubscriptions = await _subscriptionRepository.GetDueActiveSubscriptionsAsync(command.AsOfUtc, cancellationToken);
        var invoices = new List<Invoice>();

        foreach (var subscription in dueSubscriptions)
        {
            var invoice = subscription.GenerateNextInvoiceIfDue(command.AsOfUtc);
            if (invoice is not null)
            {
                invoices.Add(invoice);
            }
        }

        if (invoices.Count == 0)
        {
            return 0;
        }

        await _invoiceRepository.AddRangeAsync(invoices, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return invoices.Count;
    }
}
