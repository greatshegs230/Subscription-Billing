using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Infrastructure.Persistence;
using SubscriptionBilling.Infrastructure.Repositories;
using SubscriptionBilling.Infrastructure.Services;

namespace SubscriptionBilling.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string databaseName = "SubscriptionBillingDb")
    {
        services.AddDbContext<BillingDbContext>(options => options.UseInMemoryDatabase(databaseName));

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();

        services.AddHostedService<BillingCycleBackgroundService>();
        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }
}
