using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Internal;
using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Services;

using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

using Moq;

namespace Benchmarks.Query;

[MarkdownExporter]
[SimpleJob(3, 10, 20)]
[MemoryDiagnoser]
public class QueryParser_Benchmarks
{
    #region Static Fields and Constants

    private const string ATTRIBUTE = "Attribute";
    private const string ASCENDING_SORT = ATTRIBUTE;
    private const string DESCENDING_SORT = "-" + ATTRIBUTE;

    #endregion

    #region Constructors

    public QueryParser_Benchmarks()
    {
        var controllerContextMock = new Mock<IControllerContext>();
        controllerContextMock.Setup(m => m.RequestEntity).Returns(new ContextEntity
        {
            Attributes = new List<AttrAttribute>
            {
                new(ATTRIBUTE, ATTRIBUTE)
            }
        });

        var options = new JsonApiOptions();
        _queryParser = new BenchmarkFacade(controllerContextMock.Object, options);
    }

    #endregion

    #region Fields

    private readonly BenchmarkFacade _queryParser;

    #endregion

    #region Methods

    [Benchmark]
    public void AscendingSort()
    {
        _queryParser._ParseSortParameters(ASCENDING_SORT);
    }

    [Benchmark]
    public void DescendingSort()
    {
        _queryParser._ParseSortParameters(DESCENDING_SORT);
    }

    [Benchmark]
    public void ComplexQuery()
    {
        Run(100, () => _queryParser.Parse(
            new QueryCollection(
                new Dictionary<string, StringValues>
                {
                    { $"filter[{ATTRIBUTE}]", new StringValues(new[] { "abc", "eq:abc" }) },
                    { "sort", $"-{ATTRIBUTE}" },
                    { "include", "relationship" },
                    { "page[size]", "1" },
                    { "fields[resource]", ATTRIBUTE }
                }
            )
        ));
    }

    private void Run(int iterations, Action action)
    {
        for (var i = 0; i < iterations; i++)
            action();
    }

    #endregion

    #region Nested type: BenchmarkFacade

    // this facade allows us to expose and micro-benchmark protected methods
    private class BenchmarkFacade : QueryParser
    {
        #region Constructors

        public BenchmarkFacade(
            IControllerContext controllerContext,
            JsonApiOptions options)
            : base(controllerContext, options)
        {
        }

        #endregion

        #region Methods

        public void _ParseSortParameters(string value)
        {
            base.ParseSortParameters(value);
        }

        #endregion
    }

    #endregion
}
