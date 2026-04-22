using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SubscriptionBilling.Api.Infrastructure;

public static class RequestHashExtensions
{
    public static string ToRequestHash(this object request)
    {
        var json = JsonSerializer.Serialize(request);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}
