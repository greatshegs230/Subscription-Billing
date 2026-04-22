using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly BillingDbContext _dbContext;

    public InvoiceRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
        => _dbContext.Invoices.AddAsync(invoice, cancellationToken).AsTask();

    public Task AddRangeAsync(IEnumerable<Invoice> invoices, CancellationToken cancellationToken = default)
        => _dbContext.Invoices.AddRangeAsync(invoices, cancellationToken);

    public Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => _dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

    public async Task<IReadOnlyList<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.DueDateUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetBySubscriptionIdAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Where(x => x.SubscriptionId == subscriptionId)
            .OrderBy(x => x.DueDateUtc)
            .ToListAsync(cancellationToken);
    }
}
