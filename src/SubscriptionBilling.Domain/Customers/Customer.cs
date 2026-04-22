using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Domain.Customers;

public sealed class Customer : AggregateRoot
{
    private Customer()
    {
    }

    private Customer(Guid id, string fullName, EmailAddress email, DateTime createdAtUtc)
    {
        Id = id;
        FullName = fullName;
        Email = email;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public EmailAddress Email { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }

    public static Customer Create(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new DomainException("Customer full name is required.");
        }

        return new Customer(
            Guid.NewGuid(),
            fullName.Trim(),
            EmailAddress.Create(email),
            DateTime.UtcNow);
    }
}
