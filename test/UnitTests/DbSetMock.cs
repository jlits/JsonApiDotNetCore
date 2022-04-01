using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using Moq;

public static class DbSetMock
{
    #region Methods

    public static Mock<DbSet<T>> Create<T>(params T[] elements)
        where T : class
    {
        return new List<T>(elements).AsDbSetMock();
    }

    #endregion
}

public static class ListExtensions
{
    #region Methods

    public static Mock<DbSet<T>> AsDbSetMock<T>(this List<T> list)
        where T : class
    {
        var queryableList = list.AsQueryable();
        var dbSetMock = new Mock<DbSet<T>>();

        dbSetMock.As<IQueryable<T>>().Setup(x => x.Expression).Returns(queryableList.Expression);
        dbSetMock.As<IQueryable<T>>().Setup(x => x.ElementType).Returns(queryableList.ElementType);
        dbSetMock.As<IQueryable<T>>().Setup(x => x.GetEnumerator()).Returns(queryableList.GetEnumerator());

        dbSetMock.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(CancellationToken.None))
            .Returns(new TestAsyncEnumerator<T>(queryableList.GetEnumerator()));

        dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(queryableList.Provider));

        return dbSetMock;
    }

    #endregion
}

internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    #region Constructors

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    #endregion

    #region Fields

    private readonly IQueryProvider _inner;

    #endregion

    #region IAsyncQueryProvider Members

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
    {
        return Execute<TResult>(expression);
    }

    #endregion

    #region Methods

    public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression)
    {
        return new TestAsyncEnumerable<TResult>(expression);
    }

    #endregion
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    #region Constructors

    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    {
    }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    {
    }

    #endregion

    #region IAsyncEnumerable<T> Members

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
    {
        throw new NotImplementedException();
    }

    #endregion

    #region IQueryable<T> Members

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

    #endregion

    #region Methods

    public IAsyncEnumerator<T> GetEnumerator()
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    #endregion
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    #region Constructors

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    #endregion

    #region Fields

    private readonly IEnumerator<T> _inner;

    #endregion

    #region IAsyncEnumerator<T> Members

    public ValueTask<bool> MoveNextAsync()
    {
        throw new NotImplementedException();
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Methods

    public void Dispose()
    {
        _inner.Dispose();
    }

    public Task<bool> MoveNext(CancellationToken cancellationToken)
    {
        return Task.FromResult(_inner.MoveNext());
    }

    #endregion
}
