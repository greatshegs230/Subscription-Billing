using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.ValueObjects;

public sealed record Money
{
    public decimal Value { get; }
    public string Currency { get; }

    private Money(decimal value, string currency)
    {
        Value = value;
        Currency = currency;
    }

    public static Money Create(decimal value, string currency = "USD")
    {
        if (value <= 0)
        {
            throw new DomainException("Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        return new Money(Math.Round(value, 2, MidpointRounding.AwayFromZero), currency.Trim().ToUpperInvariant());
    }

    public override string ToString() => $"{Currency} {Value:N2}";
}
