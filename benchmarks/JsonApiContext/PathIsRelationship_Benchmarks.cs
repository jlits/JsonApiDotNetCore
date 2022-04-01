using BenchmarkDotNet.Attributes;

namespace Benchmarks.JsonApiContext;

[MarkdownExporter]
[MemoryDiagnoser]
public class PathIsRelationship_Benchmarks
{
    #region Static Fields and Constants

    private const string PATH = "https://example.com/api/v1/namespace/articles/relationships/author/";

    #endregion

    #region Methods

    [Benchmark]
    public void Current()
    {
        JsonApiDotNetCore.Services.JsonApiContext.PathIsRelationship(PATH);
    }

    [Benchmark]
    public void UsingSplit()
    {
        UsingSplitImpl(PATH);
    }

    private bool UsingSplitImpl(string path)
    {
        var split = path.Split('/');
        return split[split.Length - 2] == "relationships";
    }

    #endregion
}
