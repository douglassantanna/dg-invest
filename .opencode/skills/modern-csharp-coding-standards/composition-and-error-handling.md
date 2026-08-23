# Composition and Error Handling

Composition over inheritance, Result type pattern, and testing patterns for modern C#.

## Contents

- [Composition Over Inheritance](#composition-over-inheritance)
- [Result Type Pattern](#result-type-pattern)
- [Testing Patterns](#testing-patterns)

## Composition Over Inheritance

**Avoid abstract base classes and inheritance hierarchies.** Use composition and interfaces instead.

```csharp
// BAD: Abstract base class hierarchy
public abstract class PaymentProcessor
{
    public abstract Task<PaymentResult> ProcessAsync(Money amount);

    protected async Task<bool> ValidateAsync(Money amount)
    {
        // Shared validation logic
        return amount.Amount > 0;
    }
}

public class CreditCardProcessor : PaymentProcessor
{
    public override async Task<PaymentResult> ProcessAsync(Money amount)
    {
        await ValidateAsync(amount);
        // Process credit card...
    }
}

// GOOD: Composition with interfaces
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(Money amount, CancellationToken cancellationToken);
}

public interface IPaymentValidator
{
    Task<ValidationResult> ValidateAsync(Money amount, CancellationToken cancellationToken);
}

// Concrete implementations compose validators
public sealed class CreditCardProcessor : IPaymentProcessor
{
    private readonly IPaymentValidator _validator;
    private readonly ICreditCardGateway _gateway;

    public CreditCardProcessor(IPaymentValidator validator, ICreditCardGateway gateway)
    {
        _validator = validator;
        _gateway = gateway;
    }

    public async Task<PaymentResult> ProcessAsync(Money amount, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(amount, cancellationToken);
        if (!validation.IsValid)
            return PaymentResult.Failed(validation.Error);

        return await _gateway.ChargeAsync(amount, cancellationToken);
    }
}

// GOOD: Static helper classes for shared logic (no inheritance)
public static class PaymentValidation
{
    public static ValidationResult ValidateAmount(Money amount)
    {
        if (amount.Amount <= 0)
            return ValidationResult.Invalid("Amount must be positive");

        if (amount.Amount > 10000m)
            return ValidationResult.Invalid("Amount exceeds maximum");

        return ValidationResult.Valid();
    }
}

// GOOD: Records for modeling variants (not inheritance)
public enum PaymentType { CreditCard, BankTransfer, Cash }

public record PaymentMethod
{
    public PaymentType Type { get; init; }
    public string? Last4 { get; init; }           // For credit cards
    public string? AccountNumber { get; init; }    // For bank transfers

    public static PaymentMethod CreditCard(string last4) => new()
    {
        Type = PaymentType.CreditCard,
        Last4 = last4
    };

    public static PaymentMethod BankTransfer(string accountNumber) => new()
    {
        Type = PaymentType.BankTransfer,
        AccountNumber = accountNumber
    };

    public static PaymentMethod Cash() => new() { Type = PaymentType.Cash };
}
```

**When inheritance is acceptable:**
- Framework requirements (e.g., `ControllerBase` in ASP.NET Core)
- Library integration (e.g., custom exceptions inheriting from `Exception`)
- **These should be rare cases in your application code**

## Result Type Pattern

For expected errors, use a **generic `Result<T, TError>`** instead of exceptions. Each operation knows what success and failure look like, so the error type reflects the domain. Use enum error codes for type-safe, switchable failure classification.

```csharp
// Enum for error classification - type-safe and switchable
public enum OrderErrorCode
{
    ValidationError,
    InsufficientInventory,
    NotFound
}

// Generic result type - sealed record with factory methods
public sealed record Result<T, TError>
{
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public TError? Error { get; private init; }

    public static Result<T, TError> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T, TError> Failed(TError error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}

// Domain error carrying a code and message
public sealed record OrderError(OrderErrorCode Code, string Message);

// Usage example
public sealed class OrderService(IOrderRepository repository)
{
    public async Task<Result<Order, OrderError>> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValid(request))
            return Result<Order, OrderError>.Failure(
                new OrderError(OrderErrorCode.ValidationError, "Invalid order request"));

        if (!await HasInventoryAsync(request.Items, cancellationToken))
            return Result<Order, OrderError>.Failure(
                new OrderError(OrderErrorCode.InsufficientInventory, "Items out of stock"));

        var order = new Order(
            OrderId.New(),
            new CustomerId(request.CustomerId),
            request.Items);

        await repository.SaveAsync(order, cancellationToken);

        return Result<Order, OrderError>.Success(order);
    }

    // Map result to HTTP response - switch on enum error codes
    public IActionResult MapToActionResult(Result<Order, OrderError> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return result.Error!.Code switch
        {
            OrderErrorCode.ValidationError =>
                new BadRequestObjectResult(new { error = result.Error.Message }),
            OrderErrorCode.InsufficientInventory =>
                new ConflictObjectResult(new { error = result.Error.Message }),
            OrderErrorCode.NotFound =>
                new NotFoundObjectResult(new { error = result.Error.Message }),
            _ => new ObjectResult(new { error = result.Error.Message }) { StatusCode = 500 }
        };
    }
}
```

**When to use Result vs Exceptions:**
- **Use Result**: Expected errors (validation, business rules, not found)
- **Use Exceptions**: Unexpected errors (network failures, system errors, programming bugs)

## Testing Patterns

```csharp
// Use record for test data builders
public record OrderBuilder
{
    public OrderId Id { get; init; } = OrderId.New();
    public CustomerId CustomerId { get; init; } = CustomerId.New();
    public Money Total { get; init; } = new Money(100m, "USD");
    public IReadOnlyList<OrderItem> Items { get; init; } = Array.Empty<OrderItem>();

    public Order Build() => new(Id, CustomerId, Total, Items);
}

// Use 'with' expression for test variations
[Fact]
public void CalculateDiscount_LargeOrder_AppliesCorrectDiscount()
{
    // Arrange
    var baseOrder = new OrderBuilder().Build();
    var largeOrder = baseOrder with
    {
        Total = new Money(1500m, "USD")
    };

    // Act
    var discount = _service.CalculateDiscount(largeOrder);

    // Assert
    discount.Should().Be(new Money(225m, "USD")); // 15% of 1500
}

// Span-based testing
[Theory]
[InlineData("ORD-12345", true)]
[InlineData("INVALID", false)]
public void TryParseOrderId_VariousInputs_ReturnsExpectedResult(
    string input,
    bool expected)
{
    // Act
    var result = OrderIdParser.TryParse(input.AsSpan(), out var orderId);

    // Assert
    result.Should().Be(expected);
}

// Testing with value objects
[Fact]
public void Money_Add_SameCurrency_ReturnsSum()
{
    // Arrange
    var money1 = new Money(100m, "USD");
    var money2 = new Money(50m, "USD");

    // Act
    var result = money1.Add(money2);

    // Assert
    result.Should().Be(new Money(150m, "USD"));
}

[Fact]
public void Money_Add_DifferentCurrency_ThrowsException()
{
    // Arrange
    var usd = new Money(100m, "USD");
    var eur = new Money(50m, "EUR");

    // Act & Assert
    var act = () => usd.Add(eur);
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*different currencies*");
}
```
