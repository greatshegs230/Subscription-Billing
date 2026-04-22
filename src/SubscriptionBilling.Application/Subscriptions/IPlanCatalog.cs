using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Application.Subscriptions;

public interface IPlanCatalog
{
    bool TryGetPrice(string planCode, string billingCycle, out Money price);
}

public sealed class InMemoryPlanCatalog : IPlanCatalog
{
    private static readonly Dictionary<(string PlanCode, string BillingCycle), Money> Prices = new()
    {
        [("BASIC", "MONTHLY")] = Money.Create(10m, "USD"),
        [("BASIC", "YEARLY")] = Money.Create(100m, "USD"),
        [("PRO", "MONTHLY")] = Money.Create(25m, "USD"),
        [("PRO", "YEARLY")] = Money.Create(250m, "USD"),
        [("TEAM", "MONTHLY")] = Money.Create(50m, "USD"),
        [("TEAM", "YEARLY")] = Money.Create(500m, "USD")
    };

    public bool TryGetPrice(string planCode, string billingCycle, out Money price)
    {
        var normalizedPlanCode = (planCode ?? string.Empty).Trim().ToUpperInvariant();
        var normalizedBillingCycle = (billingCycle ?? string.Empty).Trim().ToUpperInvariant();

        return Prices.TryGetValue((normalizedPlanCode, normalizedBillingCycle), out price!);
    }
}
