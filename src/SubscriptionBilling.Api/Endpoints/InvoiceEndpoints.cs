using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Api.Infrastructure;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Application.Invoices.Commands;

namespace SubscriptionBilling.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var invoices = app.MapGroup("/invoices").WithTags("Invoices");

        invoices.MapPost("/{id:guid}/pay", PayInvoiceAsync)
            .WithName("PayInvoice")
            .WithSummary("Pay an invoice")
            .WithDescription("Marks an invoice as paid. Requires the Idempotency-Key header.");

        return app;
    }

    private static Task<IResult> PayInvoiceAsync(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IIdempotencyService idempotencyService,
        PayInvoiceCommandHandler handler,
        CancellationToken cancellationToken)
        => IdempotentEndpointExecutor.ExecuteAsync(
            idempotencyKey,
            nameof(PayInvoiceCommand),
            new { InvoiceId = id }.ToRequestHash(),
            idempotencyService,
            ct => handler.HandleAsync(new PayInvoiceCommand(id), ct),
            response => Results.Ok(response),
            "Invoice paid successfully.",
            "Invoice payment request was already processed successfully.",
            cancellationToken);
}
