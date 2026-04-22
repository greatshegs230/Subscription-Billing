using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.Customers.Commands;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Application.Subscriptions;
using SubscriptionBilling.Application.Subscriptions.Commands;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Infrastructure.Persistence;
using SubscriptionBilling.Infrastructure.Repositories;
using Xunit;

namespace SubscriptionBilling.Tests;

public sealed class InfrastructureTests
{
    [Fact]
    public async Task SaveChanges_should_create_outbox_messages_from_domain_events()
    {
        using var dbContext = CreateDbContext();
        var customerRepository = new CustomerRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var createCustomerHandler = new CreateCustomerCommandHandler(customerRepository, unitOfWork);

        var customerId = await createCustomerHandler.HandleAsync(
            new CreateCustomerCommand("Segun Aluko", "segun@example.com"));

        var subscriptionRepository = new SubscriptionRepository(dbContext);
        var invoiceRepository = new InvoiceRepository(dbContext);
        var createSubscriptionHandler = await new CreateSubscriptionCommandHandler(
    customerRepository,
    subscriptionRepository,
    invoiceRepository,
    unitOfWork,
    new InMemoryPlanCatalog()
)
.HandleAsync(new CreateSubscriptionCommand(
    customerId,
    "PRO",
    "MONTHLY",
    new DateTime(2026, 1, 1)));

        dbContext.OutboxMessages.Should().NotBeEmpty();
        dbContext.OutboxMessages.Any(x => x.Type.Contains("SubscriptionActivated")).Should().BeTrue();
        dbContext.OutboxMessages.Any(x => x.Type.Contains("InvoiceGenerated")).Should().BeTrue();
    }


    [Fact]
    public async Task Create_customer_should_reject_duplicate_email()
    {
        using var dbContext = CreateDbContext();
        var customerRepository = new CustomerRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var handler = new CreateCustomerCommandHandler(customerRepository, unitOfWork);

        await handler.HandleAsync(new CreateCustomerCommand("Segun Aluko", "dup@example.com"));

        var action = async () => await handler.HandleAsync(new CreateCustomerCommand("Another User", "dup@example.com"));

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }


[Fact]
public async Task Create_subscription_should_use_catalog_price_and_generate_matching_invoice_amount()
{
    using var dbContext = CreateDbContext();
    var customerRepository = new CustomerRepository(dbContext);
    var subscriptionRepository = new SubscriptionRepository(dbContext);
    var invoiceRepository = new InvoiceRepository(dbContext);
    var unitOfWork = new UnitOfWork(dbContext);
    var handler = new CreateSubscriptionCommandHandler(
        customerRepository,
        subscriptionRepository,
        invoiceRepository,
        unitOfWork,
        new InMemoryPlanCatalog());

    var customerId = await new CreateCustomerCommandHandler(customerRepository, unitOfWork)
        .HandleAsync(new CreateCustomerCommand("Segun Aluko", "catalog@example.com"));

    var subscriptionId = await handler.HandleAsync(
        new CreateSubscriptionCommand(customerId, "PRO", "MONTHLY", new DateTime(2026, 1, 1)));

    var invoices = await invoiceRepository.GetByCustomerIdAsync(customerId);
    invoices.Should().ContainSingle();
    invoices.Single().Amount.Value.Should().Be(25m);
    invoices.Single().Amount.Currency.Should().Be("USD");

    var subscription = await subscriptionRepository.GetByIdAsync(subscriptionId);
    subscription.Should().NotBeNull();
    subscription!.Amount.Value.Should().Be(25m);
    subscription.Amount.Currency.Should().Be("USD");
}

[Fact]
public async Task Create_subscription_should_fail_for_invalid_plan_or_billing_cycle()
{
    using var dbContext = CreateDbContext();
    var customerRepository = new CustomerRepository(dbContext);
    var subscriptionRepository = new SubscriptionRepository(dbContext);
    var invoiceRepository = new InvoiceRepository(dbContext);
    var unitOfWork = new UnitOfWork(dbContext);
    var handler = new CreateSubscriptionCommandHandler(
        customerRepository,
        subscriptionRepository,
        invoiceRepository,
        unitOfWork,
        new InMemoryPlanCatalog());

    var customerId = await new CreateCustomerCommandHandler(customerRepository, unitOfWork)
        .HandleAsync(new CreateCustomerCommand("Segun Aluko", "invalid-plan@example.com"));

    var action = async () => await handler.HandleAsync(
        new CreateSubscriptionCommand(customerId, "UNKNOWN", "MONTHLY", new DateTime(2026, 1, 1)));

    await action.Should().ThrowAsync<DomainException>()
        .WithMessage("*Invalid plan or billing cycle*");
}

    [Fact]
    public async Task Idempotency_service_should_return_same_response_on_replay()
    {
        using var dbContext = CreateDbContext();
        IIdempotencyStore store = new IdempotencyStore(dbContext);
        IIdempotencyService service = new IdempotencyService(store);

        var first = await service.ExecuteAsync(
            "abc-123",
            "CreateCustomerCommand",
            "hash-1",
            _ => Task.FromResult(Guid.NewGuid()));

        var second = await service.ExecuteAsync(
            "abc-123",
            "CreateCustomerCommand",
            "hash-1",
            _ => Task.FromResult(Guid.NewGuid()));

        second.IsReplay.Should().BeTrue();
        second.Response.Should().Be(first.Response);
    }

    [Fact]
    public async Task Idempotency_service_should_reject_same_key_with_different_payload_hash()
    {
        using var dbContext = CreateDbContext();
        IIdempotencyStore store = new IdempotencyStore(dbContext);
        IIdempotencyService service = new IdempotencyService(store);

        await service.ExecuteAsync(
            "abc-123",
            "CreateCustomerCommand",
            "hash-1",
            _ => Task.FromResult(Guid.NewGuid()));

        var action = async () => await service.ExecuteAsync(
            "abc-123",
            "CreateCustomerCommand",
            "hash-2",
            _ => Task.FromResult(Guid.NewGuid()));

        await action.Should().ThrowAsync<DomainException>()
            .WithMessage("*different request payload*");
    }

    [Fact]
    public async Task Generate_due_invoices_handler_should_generate_for_due_active_subscriptions_only()
    {
        using var dbContext = CreateDbContext();
        var customerRepository = new CustomerRepository(dbContext);
        var subscriptionRepository = new SubscriptionRepository(dbContext);
        var invoiceRepository = new InvoiceRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        var customerId = await new CreateCustomerCommandHandler(customerRepository, unitOfWork)
            .HandleAsync(new CreateCustomerCommand("Segun Aluko", "segun@example.com"));

        var subscriptionId = await new CreateSubscriptionCommandHandler(
    customerRepository,
    subscriptionRepository,
    invoiceRepository,
    unitOfWork,
    new InMemoryPlanCatalog()
)
.HandleAsync(new CreateSubscriptionCommand(
    customerId,
    "PRO",
    "MONTHLY",
    new DateTime(2026, 1, 1)));

        var handler = new GenerateDueInvoicesCommandHandler(subscriptionRepository, invoiceRepository, unitOfWork);

        var generated = await handler.HandleAsync(new GenerateDueInvoicesCommand(new DateTime(2026, 2, 1)));

        generated.Should().Be(1);
        var invoices = await invoiceRepository.GetByCustomerIdAsync(customerId);
        invoices.Should().HaveCount(2);

        var subscription = await subscriptionRepository.GetByIdAsync(subscriptionId);
        subscription!.NextBillingDateUtc.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public async Task Outbox_repository_should_track_attempts_and_processing_status()
    {
        using var dbContext = CreateDbContext();
        var customerRepository = new CustomerRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var subscriptionRepository = new SubscriptionRepository(dbContext);
        var invoiceRepository = new InvoiceRepository(dbContext);

        var customerId = await new CreateCustomerCommandHandler(customerRepository, unitOfWork)
            .HandleAsync(new CreateCustomerCommand("Segun Aluko", "segun@example.com"));

        await new CreateSubscriptionCommandHandler(
    customerRepository,
    subscriptionRepository,
    invoiceRepository,
    unitOfWork,
    new InMemoryPlanCatalog()
)
.HandleAsync(new CreateSubscriptionCommand(
    customerId,
    "PRO",
    "MONTHLY",
    new DateTime(2026, 1, 1)));

        var outboxRepository = new OutboxRepository(dbContext);
        var message = (await outboxRepository.GetUnprocessedAsync(1)).Single();

        await outboxRepository.MarkAttemptStartedAsync(message.Id);
        await outboxRepository.MarkFailedAsync(message.Id, "boom");
        await outboxRepository.MarkProcessedAsync(message.Id);

        var stored = await dbContext.OutboxMessages.FirstAsync(x => x.Id == message.Id);
        stored.AttemptCount.Should().Be(1);
        stored.LastAttemptedOnUtc.Should().NotBeNull();
        stored.ProcessedOnUtc.Should().NotBeNull();
        stored.Error.Should().BeNull();
    }

    private static BillingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BillingDbContext(options);
    }
}
