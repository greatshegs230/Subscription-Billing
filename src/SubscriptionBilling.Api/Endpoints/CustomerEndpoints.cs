using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Api.Contracts;
using SubscriptionBilling.Api.Infrastructure;
using SubscriptionBilling.Application.Customers.Commands;
using SubscriptionBilling.Application.DTOs;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Application.Invoices.Queries;

namespace SubscriptionBilling.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var customers = app.MapGroup("/customers").WithTags("Customers");

        customers.MapPost("/", CreateCustomerAsync)
            .WithName("CreateCustomer")
            .WithSummary("Create a customer")
            .WithDescription("Creates a customer. Requires the Idempotency-Key header.");

        customers.MapGet("/{id:guid}/invoices", GetCustomerInvoicesAsync)
            .WithName("GetCustomerInvoices")
            .WithSummary("Get invoices by customer")
            .WithDescription("Returns all invoices for a given customer.");

        return app;
    }

    private static Task<IResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IIdempotencyService idempotencyService,
        CreateCustomerCommandHandler handler,
        CancellationToken cancellationToken)
        => IdempotentEndpointExecutor.ExecuteAsync(
            idempotencyKey,
            nameof(CreateCustomerCommand),
            request.ToRequestHash(),
            idempotencyService,
            ct => handler.HandleAsync(new CreateCustomerCommand(request.FullName, request.Email), ct),
            response => Results.Created($"/customers/{response.Data}", response),
            "Customer created successfully.",
            "Customer request was already processed successfully.",
            cancellationToken);

    private static async Task<IResult> GetCustomerInvoicesAsync(
        Guid id,
        GetInvoicesByCustomerQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetInvoicesByCustomerQuery(id), cancellationToken);
        return Results.Ok(new ApiResponse<IReadOnlyList<InvoiceDto>>(data, "Invoices retrieved successfully."));
    }
}
