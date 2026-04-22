using FluentAssertions;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;
using SubscriptionBilling.Domain.ValueObjects;
using Xunit;

namespace SubscriptionBilling.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Create_customer_with_valid_values_should_succeed()
    {
        var customer = Customer.Create("Segun Aluko", "segun@example.com");

        customer.Id.Should().NotBeEmpty();
        customer.FullName.Should().Be("Segun Aluko");
        customer.Email.Value.Should().Be("segun@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Customer_name_cannot_be_empty(string fullName)
    {
        var action = () => Customer.Create(fullName, "segun@example.com");

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Invalid_email_should_throw()
    {
        var action = () => Customer.Create("Segun Aluko", "bad-email");

        action.Should().Throw<DomainException>()
            .WithMessage("*invalid*");
    }

    [Fact]
    public void Invalid_billing_cycle_should_throw()
    {
        var action = () => BillingCycle.Create("001");

        action.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activating_subscription_should_generate_first_invoice()
    {
        var (subscription, firstInvoice) = Subscription.CreateAndActivate(
            Guid.NewGuid(),
            "PRO",
            Money.Create(100, "USD"),
            BillingCycle.Monthly,
            new DateTime(2026, 1, 1));

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.NextBillingDateUtc.Should().Be(new DateTime(2026, 2, 1));
        firstInvoice.SubscriptionId.Should().Be(subscription.Id);
        firstInvoice.Status.Should().Be(InvoiceStatus.Pending);
        firstInvoice.DueDateUtc.Should().Be(new DateTime(2026, 1, 1));
    }

    [Fact]
    public void Billing_cycle_should_generate_next_invoice_when_due()
    {
        var (subscription, _) = Subscription.CreateAndActivate(
            Guid.NewGuid(),
            "PRO",
            Money.Create(100, "USD"),
            BillingCycle.Monthly,
            new DateTime(2026, 1, 1));

        var nextInvoice = subscription.GenerateNextInvoiceIfDue(new DateTime(2026, 2, 1));

        nextInvoice.Should().NotBeNull();
        nextInvoice!.DueDateUtc.Should().Be(new DateTime(2026, 2, 1));
        subscription.NextBillingDateUtc.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void Billing_cycle_should_not_generate_invoice_before_due_date()
    {
        var (subscription, _) = Subscription.CreateAndActivate(
            Guid.NewGuid(),
            "PRO",
            Money.Create(100, "USD"),
            BillingCycle.Monthly,
            new DateTime(2026, 1, 1));

        var nextInvoice = subscription.GenerateNextInvoiceIfDue(new DateTime(2026, 1, 31));

        nextInvoice.Should().BeNull();
        subscription.NextBillingDateUtc.Should().Be(new DateTime(2026, 2, 1));
    }

    [Fact]
    public void Cancelled_subscription_should_not_generate_future_invoice()
    {
        var (subscription, _) = Subscription.CreateAndActivate(
            Guid.NewGuid(),
            "PRO",
            Money.Create(100, "USD"),
            BillingCycle.Monthly,
            new DateTime(2026, 1, 1));

        subscription.Cancel(DateTime.UtcNow);

        var nextInvoice = subscription.GenerateNextInvoiceIfDue(new DateTime(2026, 2, 1));

        nextInvoice.Should().BeNull();
    }

    [Fact]
    public void Cancelling_subscription_twice_should_throw_domain_exception()
    {
        var (subscription, _) = Subscription.CreateAndActivate(
            Guid.NewGuid(),
            "PRO",
            Money.Create(25m, "USD"),
            BillingCycle.Create("MONTHLY"),
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        subscription.Cancel(DateTime.UtcNow);

        var action = () => subscription.Cancel(DateTime.UtcNow);

        action.Should()
            .Throw<DomainException>()
            .WithMessage("Subscription is already cancelled.");
    }

    [Fact]
    public void Paying_invoice_should_mark_it_paid()
    {
        var invoice = Invoice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(100, "USD"),
            new DateTime(2026, 1, 1));

        invoice.Pay();

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Invoice_cannot_be_paid_twice()
    {
        var invoice = Invoice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.Create(100, "USD"),
            new DateTime(2026, 1, 1));

        invoice.Pay();

        var action = () => invoice.Pay();

        action.Should().Throw<DomainException>()
            .WithMessage("*already been paid*");
    }

    [Fact]
    public void Money_cannot_be_zero_or_negative()
    {
        var action = () => Money.Create(0, "USD");

        action.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }
}
