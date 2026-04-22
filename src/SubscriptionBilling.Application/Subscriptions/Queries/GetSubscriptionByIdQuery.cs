using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.DTOs;

namespace SubscriptionBilling.Application.Subscriptions.Queries;

public sealed record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<SubscriptionDto>;
