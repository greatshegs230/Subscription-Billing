using System.Text.RegularExpressions;
using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Domain.ValueObjects;

public sealed record EmailAddress
{
    private static readonly Regex EmailRegex =
        new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email address is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
        {
            throw new DomainException("Email address is invalid.");
        }

        return new EmailAddress(normalized);
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
}
