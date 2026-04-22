using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Infrastructure.Persistence;

namespace SubscriptionBilling.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
    private readonly BillingDbContext _dbContext;

    public CustomerRepository(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => _dbContext.Customers.AddAsync(customer, cancellationToken).AsTask();

    public Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        => _dbContext.Customers.FirstOrDefaultAsync(x => x.Id == customerId, cancellationToken);

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();

        var existingCustomer = _dbContext.Customers
            .AsEnumerable()
            .FirstOrDefault(x => x.Email.Value == normalized);

        return existingCustomer;
    }
}
