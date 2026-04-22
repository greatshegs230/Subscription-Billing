using Microsoft.AspNetCore.Mvc;
using SubscriptionBilling.Api.Contracts;
using SubscriptionBilling.Api.Infrastructure;
using SubscriptionBilling.Application.DTOs;
using SubscriptionBilling.Application.Idempotency;
using SubscriptionBilling.Application.Subscriptions.Commands;
using SubscriptionBilling.Application.Subscriptions.Queries;

namespace SubscriptionBilling.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        var subscriptions = app.MapGroup("/subscriptions").WithTags("Subscriptions");

        subscriptions.MapPost("/", CreateSubscriptionAsync)
            .WithName("CreateSubscription")
            .WithSummary("Create a subscription")
            .WithDescription("Creates and activates a subscription. Requires the Idempotency-Key header.");

        subscriptions.MapGet("/{id:guid}", GetSubscriptionByIdAsync)
            .WithName("GetSubscriptionById")
            .WithSummary("Get a subscription by id")
            .WithDescription("Returns subscription details for a given subscription id.");

        subscriptions.MapPost("/{id:guid}/cancel", CancelSubscriptionAsync)
            .WithName("CancelSubscription")
            .WithSummary("Cancel a subscription")
            .WithDescription("Cancels an active subscription. Requires the Idempotency-Key header.");

        return app;
    }

    private static Task<IResult> CreateSubscriptionAsync(
        CreateSubscriptionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IIdempotencyService idempotencyService,
        CreateSubscriptionCommandHandler handler,
        CancellationToken cancellationToken)
        => IdempotentEndpointExecutor.ExecuteAsync(
            idempotencyKey,
            nameof(CreateSubscriptionCommand),
            request.ToRequestHash(),
            idempotencyService,
            ct => handler.HandleAsync(
                new CreateSubscriptionCommand(
                    request.CustomerId,
                    request.PlanCode,
                    request.BillingCycle,
                    request.StartDateUtc),
                ct),
            response => Results.Created($"/subscriptions/{response.Data}", response),
            "Subscription created successfully.",
            "Subscription create request was already processed successfully.",
            cancellationToken);

    private static async Task<IResult> GetSubscriptionByIdAsync(
        Guid id,
        GetSubscriptionByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var data = await handler.HandleAsync(new GetSubscriptionByIdQuery(id), cancellationToken);
        return Results.Ok(new ApiResponse<SubscriptionDto>(data, "Subscription retrieved successfully."));
    }

    private static Task<IResult> CancelSubscriptionAsync(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IIdempotencyService idempotencyService,
        CancelSubscriptionCommandHandler handler,
        CancellationToken cancellationToken)
        => IdempotentEndpointExecutor.ExecuteAsync(
            idempotencyKey,
            nameof(CancelSubscriptionCommand),
            new { SubscriptionId = id }.ToRequestHash(),
            idempotencyService,
            ct => handler.HandleAsync(new CancelSubscriptionCommand(id), ct),
            response => Results.Ok(response),
            "Subscription cancelled successfully.",
            "Subscription cancel was already processed successfully.",
            cancellationToken);
}
