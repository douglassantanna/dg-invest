using FluentAssertions;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using api.Cryptos.Models;
using api.Models.Cryptos;
using api.Shared;
using api.Cryptos.Commands;
using api.Data;
using api.Cryptos.TransactionStrategies.Contracts;
using System.Transactions;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace api.Tests.Cryptos.Commands;
public class AddTransactionCommandTests
{
    private readonly Mock<ILogger<AddTransactionCommandHandler>> _loggerMock;
    private readonly Mock<ITransactionService> _transactionServiceMock;
    private readonly Mock<IDataContext> _contextMock;
    private Mock<DbSet<Account>> _accountsDbSetMock;
    private readonly AddTransactionCommand _validCommand;

    public AddTransactionCommandTests()
    {
        _loggerMock = new Mock<ILogger<AddTransactionCommandHandler>>();
        _transactionServiceMock = new Mock<ITransactionService>();
        _accountsDbSetMock = new Mock<DbSet<Account>>();
        _contextMock = new Mock<IDataContext>();

        _validCommand = new AddTransactionCommand(
            Amount: 100m,
            Price: 50000m,
            PurchaseDate: DateTimeOffset.Now.AddDays(-1),
            ExchangeName: "Binance",
            TransactionType: ETransactionType.Buy,
            CryptoAssetId: 1,
            SubAccountTag: "main",
            UserId: 1,
            Fee: 0.1m
        );
    }

    [Fact]
    public void Validator_WhenCommandIsValid_ShouldPass()
    {
        // Arrange
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(_validCommand);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_WhenAmountIsInvalid_ShouldFail(decimal amount)
    {
        // Arrange
        var command = _validCommand with { Amount = amount };
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Amount");
    }

    [Fact]
    public void Validator_WhenPurchaseDateIsInFuture_ShouldFail()
    {
        // Arrange
        var command = _validCommand with { PurchaseDate = DateTimeOffset.Now.AddDays(1) };
        var validator = new AddTransactionCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "PurchaseDate");
    }

    [Fact]
    public async Task Handler_WhenAccountNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var accounts = new List<Account>().AsQueryable();
        SetupMockDbSet(_accountsDbSetMock, accounts);
        _contextMock.Setup(x => x.Accounts).Returns(_accountsDbSetMock.Object);

        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _contextMock.Object);

        // Act
        var result = await handler.Handle(_validCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        // result.Data.Should().Be(404);
    }

    [Fact]
    public async Task Handler_WhenTransactionServiceFails_ShouldReturnError()
    {
        // Arrange
        var account = new Account("main", 1);
        var accounts = new List<Account> { account }.AsQueryable();

        _accountsDbSetMock = CreateMockDbSet(accounts);
        _contextMock.Setup(x => x.Accounts).Returns(_accountsDbSetMock.Object);

        _transactionServiceMock
            .Setup(x => x.ExecuteTransaction(It.IsAny<Account>(), It.IsAny<AccountTransaction>()))
            .Returns(new Response("Transaction failed", false));

        var handler = new AddTransactionCommandHandler(_loggerMock.Object, _transactionServiceMock.Object, _contextMock.Object);

        // Act
        var result = await handler.Handle(_validCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
    {
        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
    }
    private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>()
            .Setup(m => m.Provider)
            .Returns(new AsyncQueryProvider<T>(data.Provider));

        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new AsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }
}

internal class AsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal AsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new AsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new AsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: new[] { typeof(Expression) })
            .MakeGenericMethod(resultType)
            .Invoke(this, new[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { executionResult });
    }
}

internal class AsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public AsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public AsyncEnumerable(Expression expression) : base(expression) { }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new AsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
}

internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;
    public AsyncEnumerator(IEnumerator<T> inner) => _inner = inner;
    public T Current => _inner.Current;
    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return new ValueTask();
    }
}