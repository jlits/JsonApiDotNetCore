using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace JsonApiDotNetCore.Extensions
{
    /// <summary>
    ///     Gets the current transaction or creates a new one.
    ///     If a transaction already exists, commit, rollback and dispose
    ///     will not be called. It is assumed the creator of the original
    ///     transaction should be responsible for disposal.
    /// </summary>
    internal struct SafeTransactionProxy : IDbContextTransaction
    {
        #region Fields

        private readonly bool _shouldExecute;
        private readonly IDbContextTransaction _transaction;

        #endregion

        #region Constructors

        private SafeTransactionProxy(IDbContextTransaction transaction, bool shouldExecute)
        {
            _transaction = transaction;
            _shouldExecute = shouldExecute;
        }

        #endregion

        #region Properties

        /// <inheritdoc />
        public bool SupportsSavepoints => false;

        /// <inheritdoc />
        public Guid TransactionId => _transaction.TransactionId;

        #endregion

        #region Implementations

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Commit()
        {
            Proxy(t => t.Commit());
        }

        /// <inheritdoc />
        public Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Commit();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void CreateSavepoint(string name)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task CreateSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            CreateSavepoint(name);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void ReleaseSavepoint(string name)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task ReleaseSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            ReleaseSavepoint(name);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Rollback()
        {
            Proxy(t => t.Rollback());
        }

        /// <inheritdoc />
        public Task RollbackAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            Rollback();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void RollbackToSavepoint(string name)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = new CancellationToken())
        {
            RollbackToSavepoint(name);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Proxy(t => t.Dispose());
        }

        #endregion

        #region Methods

        public static async Task<IDbContextTransaction> GetOrCreateAsync(DatabaseFacade databaseFacade)
        {
            return databaseFacade.CurrentTransaction != null
                ? new SafeTransactionProxy(databaseFacade.CurrentTransaction, false)
                : new SafeTransactionProxy(await databaseFacade.BeginTransactionAsync(), true);
        }

        private void Proxy(Action<IDbContextTransaction> func)
        {
            if (_shouldExecute)
                func(_transaction);
        }

        #endregion
    }
}
