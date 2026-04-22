using SubscriptionBilling.Application.Abstractions;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;

namespace SubscriptionBilling.Application.Customers.Commands;

public sealed class CreateCustomerCommandHandler : ICommandHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCustomerCommandHandler(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreateCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var existingCustomer = await _customerRepository.GetByEmailAsync(command.Email, cancellationToken);
        if (existingCustomer is not null)
        {
            throw new DomainException("A customer with this email already exists.");
        }

        var customer = Customer.Create(command.FullName, command.Email);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
