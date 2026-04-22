using SubscriptionBilling.Api.Contracts;
using SubscriptionBilling.Application.Idempotency;

namespace SubscriptionBilling.Api.Infrastructure;

public static class IdempotentEndpointExecutor
{
    public static async Task<IResult> ExecuteAsync<TResponse>(
        string? idempotencyKey,
        string commandName,
        string requestHash,
        IIdempotencyService idempotencyService,
        Func<CancellationToken, Task<TResponse>> action,
        Func<ApiResponse<TResponse>, IResult> resultFactory,
        string successMessage,
        string replayMessage,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Results.BadRequest(new { error = "Idempotency-Key header is required." });
        }

        try
        {
            var result = await idempotencyService.ExecuteAsync(
                idempotencyKey.Trim(),
                commandName,
                requestHash,
                action,
                cancellationToken);

            var message = result.IsReplay ? replayMessage : successMessage;
            return resultFactory(new ApiResponse<TResponse>(result.Response, message));
        }
        catch (Exception exception)
        {
            return ProblemResults.FromException(exception);
        }
    }
}
