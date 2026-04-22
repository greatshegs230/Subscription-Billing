using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;

namespace SubscriptionBilling.Application.Subscriptions.Commands;

public sealed class CreateSubscriptionCommandHandler : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlanCatalog _planCatalog;

    public CreateSubscriptionCommandHandler(
        ICustomerRepository customerRepository,
        ISubscriptionRepository subscriptionRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        IPlanCatalog planCatalog)
    {
        _customerRepository = customerRepository;
        _subscriptionRepository = subscriptionRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
        _planCatalog = planCatalog;
    }

    public async Task<Guid> HandleAsync(CreateSubscriptionCommand command, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new DomainException("Customer was not found.");
        }

        if (await _subscriptionRepository.ExistsActiveForCustomerPlanAsync(command.CustomerId, command.PlanCode, cancellationToken))
        {
            throw new DomainException("An active subscription already exists for this customer and plan.");
        }

        if (!_planCatalog.TryGetPrice(command.PlanCode, command.BillingCycle, out var amount))
        {
            throw new DomainException("Invalid plan or billing cycle.");
        }

        var billingCycle = BillingCycle.Create(command.BillingCycle);

        var (subscription, firstInvoice) = Subscription.CreateAndActivate(
            command.CustomerId,
            command.PlanCode,
            amount,
            billingCycle,
            command.StartDateUtc);

        await _subscriptionRepository.AddAsync(subscription, cancellationToken);
        await _invoiceRepository.AddAsync(firstInvoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}
