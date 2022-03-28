using System;
using System.Linq;
using System.Threading.Tasks;

using JsonApiDotNetCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JsonApiDotNetCore.Extensions
{
    public static class DbContextExtensions
    {
        #region Methods

        /// <summary>
        ///     Determines whether or not EF is already tracking an entity of the same Type and Id
        /// </summary>
        public static bool EntityIsTracked(this DbContext context, IIdentifiable entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var trackedEntries = context.ChangeTracker
                .Entries()
                .FirstOrDefault(entry =>
                    entry.Entity.GetType() == entity.GetType()
                    && ((IIdentifiable)entry.Entity).StringId == entity.StringId
                );

            return trackedEntries != null;
        }

        /// <summary>
        ///     Gets the current transaction or creates a new one.
        ///     If a transaction already exists, commit, rollback and dispose
        ///     will not be called. It is assumed the creator of the original
        ///     transaction should be responsible for disposal.
        /// </summary>
        /// <example>
        ///     <code>
        /// using(var transaction = _context.GetCurrentOrCreateTransaction())
        /// {
        ///     // perform multiple operations on the context and then save...
        ///     _context.SaveChanges();
        /// }
        /// </code>
        /// </example>
        public static async Task<IDbContextTransaction> GetCurrentOrCreateTransactionAsync(this DbContext context)
        {
            return await SafeTransactionProxy.GetOrCreateAsync(context.Database);
        }

        [Obsolete("This is no longer required since the introduction of context.Set<T>", false)]
        public static DbSet<T> GetDbSet<T>(this DbContext context)
            where T : class
        {
            return context.Set<T>();
        }

        /// <summary>
        ///     Get the DbSet when the model type is unknown until runtime
        /// </summary>
        public static IQueryable<object> Set(this DbContext context, Type t)
        {
            return (IQueryable<object>)context
                .GetType()
                .GetMethod("Set")
                .MakeGenericMethod(t) // TODO: will caching help runtime performance?
                .Invoke(context, null);
        }

        #endregion
    }
}
