using Microsoft.EntityFrameworkCore;
using SubscriptionBilling.Api.Endpoints;
using SubscriptionBilling.Application;
using SubscriptionBilling.Infrastructure;
using SubscriptionBilling.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();


app.MapCustomerEndpoints();
app.MapSubscriptionEndpoints();
app.MapInvoiceEndpoints();

app.Run();

public partial class Program;
