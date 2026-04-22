using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Customers.Commands;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Application.Invoices.Commands;
using SubscriptionBilling.Application.Invoices.Queries;
using SubscriptionBilling.Application.Subscriptions;
using SubscriptionBilling.Application.Subscriptions.Commands;
using SubscriptionBilling.Application.Subscriptions.Queries;

namespace SubscriptionBilling.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IPlanCatalog, InMemoryPlanCatalog>();
        services.AddScoped<CreateCustomerCommandHandler>();
        services.AddScoped<CreateSubscriptionCommandHandler>();
        services.AddScoped<CancelSubscriptionCommandHandler>();
        services.AddScoped<PayInvoiceCommandHandler>();
        services.AddScoped<GenerateDueInvoicesCommandHandler>();
        services.AddScoped<GetSubscriptionByIdQueryHandler>();
        services.AddScoped<GetInvoicesByCustomerQueryHandler>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        return services;
    }
}
