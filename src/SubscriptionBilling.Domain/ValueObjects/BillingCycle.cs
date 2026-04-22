using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.ValueObjects;

public sealed record BillingCycle
{
    public static readonly BillingCycle Monthly = new("MONTHLY");
    public static readonly BillingCycle Quarterly = new("QUARTERLY");
    public static readonly BillingCycle Yearly = new("YEARLY");

    private BillingCycle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static IReadOnlyCollection<string> AllowedValues { get; } =
        new[] { Monthly.Value, Quarterly.Value, Yearly.Value };

    public static BillingCycle Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Billing cycle is required.");
        }

        return value.Trim().ToUpperInvariant() switch
        {
            "MONTHLY" => Monthly,
            "QUARTERLY" => Quarterly,
            "YEARLY" => Yearly,
            _ => throw new DomainException("Unsupported billing cycle.")
        };
    }

    public DateTime CalculateNextBillingDate(DateTime currentDateUtc)
    {
        var date = currentDateUtc.Date;

        return Value switch
        {
            "MONTHLY" => date.AddMonths(1),
            "QUARTERLY" => date.AddMonths(3),
            "YEARLY" => date.AddYears(1),
            _ => throw new DomainException("Unsupported billing cycle.")
        };
    }

    public override string ToString() => Value;
}
