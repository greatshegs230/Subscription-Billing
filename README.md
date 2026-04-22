# SubscriptionBilling

This solution implements the Journey Mentor backend challenge as a small, readable .NET 8 codebase built with DDD, Clean Architecture, CQRS, Minimal API, and EF Core InMemory. It keeps the core domain behavior inside the aggregates and keeps the API and infrastructure layers lightweight. The challenge requires Customer, Subscription, and Invoice aggregates; strict business rules around activation, billing, payment, and cancellation; Minimal API endpoints; EF Core persistence; and unit tests, with bonus support for background billing, outbox, and idempotent commands.

## How this solution maps to the task

The challenge asks for:

* `Customer`, `Subscription`, and `Invoice` as aggregates
* entities and value objects with enforced invariants
* first invoice created on subscription activation
* a new invoice generated each billing cycle
* invoice payment marking the invoice as paid
* cancellation stopping future invoices while keeping history
* prevention of double payment
* Minimal API endpoints to create customer, create or cancel subscription, pay invoice, and get invoices
* EF Core persistence, where InMemory is acceptable
* unit tests for domain logic
* bonus support for background billing, outbox, and idempotent commands

This solution implements each of those items directly. fileciteturn6file0L1-L18

## Solution structure

* `src/SubscriptionBilling.Domain`

  * aggregates, value objects, domain events, and domain exceptions
* `src/SubscriptionBilling.Application`

  * command handlers, query handlers, repository abstractions, and idempotency orchestration
* `src/SubscriptionBilling.Infrastructure`

  * EF Core InMemory persistence, repository implementations, outbox persistence, and background services
* `src/SubscriptionBilling.Api`

  * Minimal API endpoint modules and startup wiring
* `tests/SubscriptionBilling.Tests`

  * domain, infrastructure, and API tests

## Aggregate boundaries

### Customer aggregate

Responsible for customer identity and customer invariants:

* non-empty full name
* valid email address

### Subscription aggregate

Responsible for subscription lifecycle and billing cadence:

* activation
* cancellation
* next billing date tracking
* deciding whether a due invoice should be generated

### Invoice aggregate

Responsible for invoice payment state:

* pending vs paid
* preventing double payment
* raising payment-related domain events

## Design decisions

* **Rich domain model**: business rules live in the aggregates, not in controllers or handlers.
* **Clean Architecture**: the domain has no dependency on application, infrastructure, or API.
* **CQRS**: write operations use commands and handlers; invoice retrieval uses a query and handler.
* **Minimal API**: endpoints are split into modules so 'Program.cs' stays small.
* **EF Core InMemory**: used because the challenge explicitly allows it and it keeps setup lightweight.
* **Outbox**: domain events are captured during 'SaveChangesAsync()' and persisted to an outbox table.
* **Idempotency**: write endpoints require an 'Idempotency-Key' header and replay the stored response for duplicate retries using the same request payload.
* **Plan pricing**: subscription price is derived from a small server-side plan catalog so callers cannot choose arbitrary amounts for a plan and billing cycle.

## Business rules implemented

* activating a subscription generates the first invoice
* each billing cycle generates a new invoice
* paying an invoice marks it as paid
* cancelling a subscription stops future invoices
* invoice history remains after cancellation
* an invoice cannot be paid twice
* customer email must be unique
* a customer cannot have more than one active subscription for the same plan
* subscription price is controlled by the server using 'planCode' and 'billingCycle'

## API endpoints

* `POST /customers`
* `POST /subscriptions`
* `POST /subscriptions/{id}/cancel`
* `POST /invoices/{id}/pay`
* `GET /customers/{id}/invoices`

## Run locally

```bash
dotnet restore
dotnet build
dotnet run --project src/SubscriptionBilling.Api
```

Swagger is available when the API starts.

## Run tests

```bash
dotnet test
```

## Example requests

### Create customer

Header:

* `Idempotency-Key: customer-001`

Body:

```json
{
  "fullName": "Segun Aluko",
  "email": "segun@example.com"
}
```

### Create subscription

Header:

* `Idempotency-Key: subscription-001`

Body:

```json
{
  "customerId": "PUT-CUSTOMER-ID",
  "planCode": "PRO",
  "billingCycle": "MONTHLY",
  "startDateUtc": "2026-01-01T00:00:00Z"
}
```

The API derives the amount from a server-side catalog. For example, `PRO + MONTHLY` is priced as `USD 25.00`.

```

### Pay invoice
Header:
- `Idempotency-Key: pay-001`

### Cancel subscription
Header:
- `Idempotency-Key: cancel-001`

## Trade-offs

- EF Core InMemory keeps the solution very easy to run.
- My code favors readability and directness over introducing extra abstraction layers that are not needed.


## Additional endpoint

- `GET /subscriptions/{id}` returns the current subscription state, including status, next billing date, and cancellation timestamp.

- Response messages now describe the result of each command and indicate when a request was replayed.

## Design Decisions

### 1. Domain-Driven Design (DDD)

The system models `Customer`, `Subscription`, and `Invoice` as domain entities with enforced invariants.

- Business rules such as “invoice cannot be paid twice” and “cancellation stops future billing” are enforced inside the domain rather than in endpoints.
- This keeps business behavior centralized, testable, and resistant to accidental controller/service drift.

### 2. Invoice Modeling Choice

Invoices are modeled as their own aggregate roots for this solution.

Reasoning:
- Payment is an important lifecycle of its own and can be queried independently.
- Invoices are useful as historical financial records even after subscription cancellation.


Trade-off:
- This adds a little more coordination when a subscription generates invoices.
- For a smaller system, invoice could be treated as an internal child of subscription, but using it as an aggregate here keeps financial state explicit.

### 3. CQRS (Command-Query Separation)

Writes are handled through explicit command handlers, while reads use focused query handlers.

Why:
- Keeps mutation logic isolated and expressive.
- Makes business workflows easier to reason about.
- Avoids over-engineering the read side for a take-home challenge.

### 4. Idempotency Handling

All POST endpoints require an `Idempotency-Key`.

\*\*Behavior:\*\*
- Same key + same payload → returns the cached successful response
- Same key + different payload → rejected with HTTP 409

Why:
- Makes retries safe, especially for payment and billing operations.
- Prevents duplicate customer creation, duplicate subscription creation, and duplicate payment side effects.

### 5. Server-Side Pricing Enforcement

Subscription pricing is derived from a server-side plan catalog using `planCode` + `billingCycle`.

\*\*Why:\*\*
- Prevents client-side manipulation of billing amounts.
- Keeps pricing rules owned by the system rather than trusting the request body.
- Makes the billing model feel more realistic without introducing unnecessary complexity.

### 6. Domain Events and Outbox Pattern

Aggregates raise domain events such as:
- `SubscriptionActivated`
- `InvoiceGenerated`
- `PaymentReceived`

These events are persisted to an outbox table during the same save operation.

Why:
- Preserves intent in the domain model.
- Provides a reliable handoff point for asynchronous processing.
- Avoids the classic problem of updating the database successfully but losing the event.

### 7. Clean Architecture Boundaries

The solution is split into:
- \*\*API\*\* for HTTP concerns
- \*\*Application\*\* for use cases and orchestration
- \*\*Domain\*\* for business rules and model behavior
- \*\*Infrastructure\*\* for EF Core, repositories, idempotency persistence, and outbox persistence

Why:
- Keeps framework concerns out of the domain.
- Makes the codebase easier to evolve and review.
- Helps the solution stay interview-friendly and maintainable.

### 8. Error Handling Strategy

Errors are intentionally business-focused and descriptive.

Examples:
- `Duplicate payment attempt: invoice already paid.`
- `Customer with this email already exists.`
- `Request already processed successfully. Returning cached response (idempotent replay).`

Why:
- Improves API usability.
- Makes failures easier to understand during testing and review.
- Better reflects real-world billing and payment language than generic server errors.

### 9. Simplicity Over Over-Engineering

The solution deliberately stays simple where the brief does not require extra complexity.

Examples:
- EF Core InMemory is used for local execution and tests.
- The pricing catalog is in-memory rather than persisted.
- The API remains minimal and task-focused.

Why:
- Keeps the implementation aligned with the challenge scope.


