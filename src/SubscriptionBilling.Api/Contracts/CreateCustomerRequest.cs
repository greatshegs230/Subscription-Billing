namespace SubscriptionBilling.Api.Contracts;

public sealed record CreateCustomerRequest(string FullName, string Email);
