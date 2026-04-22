using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Domain.Common;
using SubscriptionBilling.Domain.Customers;
using SubscriptionBilling.Domain.Invoices;
using SubscriptionBilling.Domain.Subscriptions;

namespace SubscriptionBilling.Infrastructure.Persistence;

public sealed class BillingDbContext : DbContext
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();

            builder.OwnsOne(x => x.Email, owned =>
            {

                owned.Property(x => x.Value)
                    .HasMaxLength(200)
                    .IsRequired();
            });

        });

        modelBuilder.Entity<Subscription>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.PlanCode).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.StartDateUtc).IsRequired();
            builder.Property(x => x.NextBillingDateUtc);
            builder.Property(x => x.CancelledAtUtc);

            builder.OwnsOne(x => x.Amount, owned =>
            {

                owned.Property(x => x.Value).IsRequired();
                owned.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            });

            builder.OwnsOne(x => x.BillingCycle, owned =>
            {

                owned.Property(x => x.Value)
                    .HasMaxLength(50)
                    .IsRequired();
            });

            builder.HasIndex(x => new { x.CustomerId, x.PlanCode, x.Status });
        });

        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.SubscriptionId).IsRequired();
            builder.Property(x => x.CustomerId).IsRequired();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.DueDateUtc).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.PaidAtUtc);

            builder.OwnsOne(x => x.Amount, owned =>
            {

                owned.Property(x => x.Value).IsRequired();
                owned.Property(x => x.Currency).HasMaxLength(10).IsRequired();
            });

            builder.HasIndex(x => new { x.CustomerId, x.DueDateUtc });
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.OccurredOnUtc).IsRequired();
            builder.Property(x => x.ProcessedOnUtc);
            builder.Property(x => x.LastAttemptedOnUtc);
            builder.Property(x => x.AttemptCount).IsRequired();
            builder.Property(x => x.Error);
            builder.HasIndex(x => x.ProcessedOnUtc);
        });

        modelBuilder.Entity<IdempotencyRecord>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Key).HasMaxLength(200).IsRequired();
            builder.Property(x => x.CommandName).HasMaxLength(200).IsRequired();
            builder.Property(x => x.RequestHash).HasMaxLength(200).IsRequired();
            builder.Property(x => x.ResponseJson).IsRequired();
            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.HasIndex(x => new { x.Key, x.CommandName }).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = ChangeTracker
            .Entries<Entity>()
            .Where(entry => entry.Entity.DomainEvents.Any())
            .Select(entry => entry.Entity)
            .ToList();

        var outboxMessages = domainEntities
            .SelectMany(entity => entity.DomainEvents)
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.EventId,
                Type = domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                OccurredOnUtc = domainEvent.OccurredOnUtc,
                AttemptCount = 0
            })
            .ToList();

        if (outboxMessages.Count > 0)
        {
            foreach (var message in outboxMessages)
            {
                var alreadyTracked = ChangeTracker.Entries<OutboxMessage>().Any(x => x.Entity.Id == message.Id);
                if (!alreadyTracked)
                {
                    OutboxMessages.Add(message);
                }
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }

        return result;
    }
}
