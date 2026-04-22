using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SubscriptionBilling.Api.Contracts;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Application.DTOs;
using SubscriptionBilling.Infrastructure.Persistence;
using SubscriptionBilling.Infrastructure.Repositories;
using Xunit;

namespace SubscriptionBilling.Tests;

public sealed class ApiIntegrationTests
{
    private static string UniqueEmail(string prefix) => $"{prefix}-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Create_customer_should_require_idempotency_header()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var response = await client.PostAsJsonAsync(
            "/customers",
            new CreateCustomerRequest("Segun Aluko", "segun@example.com"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_customer_should_replay_same_response_for_same_idempotency_key()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var request = new CreateCustomerRequest("Segun Aluko", UniqueEmail("replay-customer"));

        var first = await PostJsonAsync(client, "/customers", request, "customer-1");
        var second = await PostJsonAsync(client, "/customers", request, "customer-1");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstPayload = await first.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var secondPayload = await second.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        firstPayload.Should().NotBeNull();
        secondPayload.Should().NotBeNull();
        secondPayload!.Message.Should().ContainEquivalentOf("already processed");
        secondPayload.Data.Should().Be(firstPayload!.Data);
    }

    [Fact]
    public async Task Create_customer_should_reject_same_idempotency_key_for_different_payload()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var first = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Segun Aluko", UniqueEmail("customer-key-different-first")),
            "customer-2");

        var second = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Different User", "different@example.com"),
            "customer-2");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_customer_should_reject_duplicate_email_even_with_different_idempotency_key()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        const string email = "duplicate@example.com";

        var first = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Segun Aluko", email),
            "customer-duplicate-1");

        var second = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Segun Aluko", email),
            "customer-duplicate-2");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_subscription_should_replay_same_response_for_same_idempotency_key()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var customerId = await CreateCustomerAsync(client, "customer-subscription-1", "sub1@example.com");
        var request = new CreateSubscriptionRequest(
            customerId,
            "PRO",
            "MONTHLY",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var first = await PostJsonAsync(client, "/subscriptions", request, "subscription-1");
        var second = await PostJsonAsync(client, "/subscriptions", request, "subscription-1");

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstPayload = await first.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        var secondPayload = await second.Content.ReadFromJsonAsync<ApiResponse<Guid>>();

        firstPayload.Should().NotBeNull();
        secondPayload.Should().NotBeNull();
        secondPayload!.Message.Should().ContainEquivalentOf("already processed");
        secondPayload.Data.Should().Be(firstPayload!.Data);
    }

    [Fact]
    public async Task Create_subscription_should_use_server_side_catalog_price()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var customerId = await CreateCustomerAsync(client, "customer-subscription-catalog", "catalog-sub@example.com");

        var response = await PostJsonAsync(
            client,
            "/subscriptions",
            new CreateSubscriptionRequest(
                customerId,
                "PRO",
                "MONTHLY",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            "subscription-catalog-1");

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoicesResponse = await client.GetAsync($"/customers/{customerId}/invoices");
        invoicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoicesPayload = await invoicesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<InvoiceDto>>>();
        invoicesPayload.Should().NotBeNull();
        invoicesPayload!.Data.Should().ContainSingle();

        var invoice = invoicesPayload.Data.Single();
        invoice.Amount.Should().Be(25m);
        invoice.Currency.Should().Be("USD");
    }

    [Fact]
    public async Task Create_subscription_should_reject_invalid_plan()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var customerId = await CreateCustomerAsync(client, "customer-subscription-invalid", "invalid-sub@example.com");

        var response = await PostJsonAsync(
            client,
            "/subscriptions",
            new CreateSubscriptionRequest(
                customerId,
                "UNKNOWN",
                "MONTHLY",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            "subscription-invalid-1");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Pay_invoice_should_replay_same_response_for_same_idempotency_key()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var (customerId, _) = await CreateCustomerAndSubscriptionAsync(client, "invoice-replay@example.com");
        var invoiceId = await GetSingleInvoiceIdAsync(client, customerId);

        var first = await PostEmptyAsync(client, $"/invoices/{invoiceId}/pay", "pay-1");
        var second = await PostEmptyAsync(client, $"/invoices/{invoiceId}/pay", "pay-1");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPayload = await second.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        secondPayload.Should().NotBeNull();
        secondPayload!.Message.Should().ContainEquivalentOf("already processed");
        secondPayload.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Paying_same_invoice_twice_with_different_keys_should_fail_on_second_attempt()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var (customerId, _) = await CreateCustomerAndSubscriptionAsync(client, "invoice-paytwice@example.com");
        var invoiceId = await GetSingleInvoiceIdAsync(client, customerId);

        var first = await PostEmptyAsync(client, $"/invoices/{invoiceId}/pay", "pay-twice-1");
        var second = await PostEmptyAsync(client, $"/invoices/{invoiceId}/pay", "pay-twice-2");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_subscription_should_replay_same_response_for_same_idempotency_key()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var (_, subscriptionId) = await CreateCustomerAndSubscriptionAsync(client, "cancel-replay@example.com");

        var first = await PostEmptyAsync(client, $"/subscriptions/{subscriptionId}/cancel", "cancel-1");
        var second = await PostEmptyAsync(client, $"/subscriptions/{subscriptionId}/cancel", "cancel-1");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPayload = await second.Content.ReadFromJsonAsync<ApiResponse<bool>>();
        secondPayload.Should().NotBeNull();
        secondPayload!.Message.Should().ContainEquivalentOf("already processed");
        secondPayload.Data.Should().BeTrue();
    }

    [Fact]
    public async Task Subscription_lifecycle_should_create_and_pay_invoices()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var createCustomerResponse = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Segun Aluko", UniqueEmail("lifecycle-customer")),
            "customer-3");

        createCustomerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var customerPayload = await createCustomerResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        customerPayload.Should().NotBeNull();
        var customerId = customerPayload!.Data;

        var createSubscriptionResponse = await PostJsonAsync(
            client,
            "/subscriptions",
            new CreateSubscriptionRequest(
                customerId,
                "PRO",
                "MONTHLY",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            "subscription-2");

        createSubscriptionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var invoicesResponse = await client.GetAsync($"/customers/{customerId}/invoices");
        invoicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoicesPayload = await invoicesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<InvoiceDto>>>();
        invoicesPayload.Should().NotBeNull();
        var invoices = invoicesPayload!.Data.ToList();
        invoices.Should().HaveCount(1);

        var payResponse = await PostEmptyAsync(client, $"/invoices/{invoices[0].Id}/pay", "pay-2");
        payResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paidInvoicesResponse = await client.GetAsync($"/customers/{customerId}/invoices");
        paidInvoicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var paidInvoicesPayload = await paidInvoicesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<InvoiceDto>>>();
        paidInvoicesPayload.Should().NotBeNull();
        paidInvoicesPayload!.Data.Single().Status.Should().Be("Paid");
    }

    [Fact]
    public async Task Get_subscription_by_id_should_return_subscription_details()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var (_, subscriptionId) = await CreateCustomerAndSubscriptionAsync(client, "subscription-query@example.com");

        var response = await client.GetAsync($"/subscriptions/{subscriptionId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SubscriptionDto>>();
        payload.Should().NotBeNull();
        payload!.Data.Id.Should().Be(subscriptionId);
        payload.Data.PlanCode.Should().Be("PRO");
        payload.Data.BillingCycle.Should().Be("MONTHLY");
        payload.Message.Should().ContainEquivalentOf("retrieved");
    }

    [Fact]
    public async Task Get_subscription_by_id_should_show_cancelled_status_after_cancellation()
    {
        using var testContext = CreateTestContext();
        using var client = testContext.Client;

        var (_, subscriptionId) = await CreateCustomerAndSubscriptionAsync(client, "subscription-cancel-check@example.com");

        var cancelResponse = await PostEmptyAsync(client, $"/subscriptions/{subscriptionId}/cancel", "cancel-check-1");
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/subscriptions/{subscriptionId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await getResponse.Content.ReadFromJsonAsync<ApiResponse<SubscriptionDto>>();
        payload.Should().NotBeNull();
        payload!.Data.Status.Should().Be("Cancelled");
        payload.Data.CancelledAtUtc.Should().NotBeNull();
    }

    private static async Task<Guid> CreateCustomerAsync(HttpClient client, string idempotencyKey, string email)
    {
        var response = await PostJsonAsync(
            client,
            "/customers",
            new CreateCustomerRequest("Segun Aluko", email),
            idempotencyKey);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        payload.Should().NotBeNull();

        return payload!.Data;
    }

    private static async Task<(Guid CustomerId, Guid SubscriptionId)> CreateCustomerAndSubscriptionAsync(HttpClient client, string email)
    {
        var customerId = await CreateCustomerAsync(client, $"cust-{Guid.NewGuid():N}", email);

        var subscriptionResponse = await PostJsonAsync(
            client,
            "/subscriptions",
            new CreateSubscriptionRequest(
                customerId,
                "PRO",
                "MONTHLY",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            $"sub-{Guid.NewGuid():N}");

        subscriptionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var subscriptionPayload = await subscriptionResponse.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        subscriptionPayload.Should().NotBeNull();

        return (customerId, subscriptionPayload!.Data);
    }

    private static async Task<Guid> GetSingleInvoiceIdAsync(HttpClient client, Guid customerId)
    {
        var invoicesResponse = await client.GetAsync($"/customers/{customerId}/invoices");
        invoicesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var invoicesPayload = await invoicesResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<InvoiceDto>>>();
        invoicesPayload.Should().NotBeNull();
        invoicesPayload!.Data.Should().ContainSingle();

        return invoicesPayload.Data.First().Id;
    }

    private static async Task<HttpResponseMessage> PostJsonAsync<TRequest>(HttpClient client, string url, TRequest body, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string url, string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static TestContext CreateTestContext()
        => new(new SubscriptionBillingApiFactory());

    private sealed class TestContext : IDisposable
    {
        public TestContext(SubscriptionBillingApiFactory factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
        }

        public SubscriptionBillingApiFactory Factory { get; }
        public HttpClient Client { get; }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }
}

public sealed class SubscriptionBillingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"SubscriptionBillingTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<BillingDbContext>();
            services.RemoveAll<DbContextOptions<BillingDbContext>>();
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IOutboxRepository>();
            services.RemoveAll<IIdempotencyStore>();

            services.AddDbContext<BillingDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        });
    }
}
