using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Api.Infrastructure;

public static class ProblemResults
{
    public static IResult FromException(Exception exception)
        => exception switch
        {
            DomainException => Results.Problem(
                title: "Business rule violated",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict),
            KeyNotFoundException => Results.Problem(
                title: "Resource not found",
                detail: exception.Message,
                statusCode: StatusCodes.Status404NotFound),
            ArgumentException => Results.Problem(
                title: "Invalid request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest),
            _ => Results.Problem(
                title: "Request failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest)
        };
}
