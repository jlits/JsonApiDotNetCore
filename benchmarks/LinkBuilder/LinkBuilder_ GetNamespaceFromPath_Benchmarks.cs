using BenchmarkDotNet.Attributes;

namespace Benchmarks.LinkBuilder;

[MarkdownExporter]
[SimpleJob(3, 10, 20)]
[MemoryDiagnoser]
public class LinkBuilder_GetNamespaceFromPath_Benchmarks
{
    #region Static Fields and Constants

    private const string PATH = "/api/some-really-long-namespace-path/resources/current/articles";
    private const string ENTITY_NAME = "articles";

    #endregion

    #region Methods

    [Benchmark]
    public void UsingSplit()
    {
        GetNamespaceFromPath_BySplitting(PATH, ENTITY_NAME);
    }

    [Benchmark]
    public void Current()
    {
        GetNameSpaceFromPath_Current(PATH, ENTITY_NAME);
    }

    public static string GetNamespaceFromPath_BySplitting(string path, string entityName)
    {
        var nSpace = string.Empty;
        var segments = path.Split('/');

        for (var i = 1; i < segments.Length; i++)
        {
            if (segments[i].ToLower() == entityName)
                break;

            nSpace += $"/{segments[i]}";
        }

        return nSpace;
    }

    public static string GetNameSpaceFromPath_Current(string path, string entityName)
    {
        return JsonApiDotNetCore.Builders.LinkBuilder.GetNamespaceFromPath(path, entityName);
    }

    #endregion
}
