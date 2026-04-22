using System.Collections.Concurrent;
using System.Text.Json;
using SubscriptionBilling.Application.Abstractions.Repositories;
using SubscriptionBilling.Domain.Common;

namespace SubscriptionBilling.Application.Idempotency;

public sealed class IdempotencyService : IIdempotencyService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
    private readonly IIdempotencyStore _idempotencyStore;

    public IdempotencyService(IIdempotencyStore idempotencyStore)
    {
        _idempotencyStore = idempotencyStore;
    }

    public async Task<IdempotencyResult<TResponse>> ExecuteAsync<TResponse>(
        string key,
        string commandName,
        string requestHash,
        Func<CancellationToken, Task<TResponse>> action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(commandName))
        {
            throw new ArgumentException("Command name is required.", nameof(commandName));
        }

        if (string.IsNullOrWhiteSpace(requestHash))
        {
            throw new ArgumentException("Request hash is required.", nameof(requestHash));
        }

        key = key.Trim();
        commandName = commandName.Trim();
        requestHash = requestHash.Trim();

        var lockKey = $"{commandName}:{key}";
        var gate = Locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            var existing = await _idempotencyStore.GetAsync(key, commandName, cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    throw new DomainException("The supplied Idempotency-Key has already been used with a different request payload.");
                }

                var replayResponse = JsonSerializer.Deserialize<TResponse>(existing.ResponseJson, SerializerOptions)
                    ?? throw new InvalidOperationException("Stored idempotent response could not be deserialized.");

                return new IdempotencyResult<TResponse>(replayResponse, true);
            }

            var response = await action(cancellationToken);
            var responseJson = JsonSerializer.Serialize(response, SerializerOptions);

            await _idempotencyStore.SaveAsync(
                new IdempotencyRecordDto(Guid.NewGuid(), key, commandName, requestHash, responseJson, DateTime.UtcNow),
                cancellationToken);

            return new IdempotencyResult<TResponse>(response, false);
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
            {
                Locks.TryRemove(lockKey, out _);
            }
        }
    }
}
