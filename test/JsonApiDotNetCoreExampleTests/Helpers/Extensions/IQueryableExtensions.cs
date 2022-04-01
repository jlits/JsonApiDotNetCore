using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace JsonApiDotNetCoreExampleTests.Helpers.Extensions;

public static class IQueryableExtensions
{
    #region Static Fields and Constants

    private static readonly FieldInfo QueryCompilerField =
        typeof(EntityQueryProvider).GetTypeInfo().DeclaredFields.Single(x => x.Name == "_queryCompiler");

    private static readonly TypeInfo QueryCompilerTypeInfo = typeof(QueryCompiler).GetTypeInfo();

    private static readonly FieldInfo QueryModelGeneratorField =
        QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_queryModelGenerator");

    private static readonly FieldInfo DatabaseField =
        QueryCompilerTypeInfo.DeclaredFields.Single(x => x.Name == "_database");

    private static readonly PropertyInfo DependenciesProperty =
        typeof(Database).GetTypeInfo().DeclaredProperties.Single(x => x.Name == "Dependencies");

    #endregion

    #region Methods

    public static string ToSql<TEntity>(this IQueryable<TEntity> queryable)
        where TEntity : class
    {
        if (!(queryable is EntityQueryable<TEntity>) && !(queryable is InternalDbSet<TEntity>))
            throw new ArgumentException();

        using var enumerator = queryable.Provider.Execute<IEnumerable<TEntity>>(queryable.Expression).GetEnumerator();
        var enumeratorType = enumerator.GetType();
        var selectFieldInfo =
            enumeratorType.GetField("_selectExpression", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"cannot find field _selectExpression on type {enumeratorType.Name}");

        var sqlGeneratorFieldInfo =
            enumeratorType.GetField("_querySqlGeneratorFactory", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException(
                $"cannot find field _querySqlGeneratorFactory on type {enumeratorType.Name}");

        var selectExpression = selectFieldInfo.GetValue(enumerator) as SelectExpression
            ?? throw new InvalidOperationException("could not get SelectExpression");

        var factory = sqlGeneratorFieldInfo.GetValue(enumerator) as IQuerySqlGeneratorFactory
            ?? throw new InvalidOperationException("could not get IQuerySqlGeneratorFactory");

        var sqlGenerator = factory.Create();
        var command = sqlGenerator.GetCommand(selectExpression);
        var sql = command.CommandText;
        return sql;
    }

    #endregion
}
