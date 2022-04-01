using BenchmarkDotNet.Attributes;

using JsonApiDotNetCore.Internal;

namespace Benchmarks.RequestMiddleware;

[MarkdownExporter]
[MemoryDiagnoser]
public class ContainsMediaTypeParameters_Benchmarks
{
    #region Static Fields and Constants

    private const string MEDIA_TYPE = "application/vnd.api+json; version=1";

    #endregion

    #region Methods

    [Benchmark]
    public void UsingSplit()
    {
        UsingSplitImpl(MEDIA_TYPE);
    }

    [Benchmark]
    public void Current()
    {
        JsonApiDotNetCore.Middleware.RequestMiddleware.ContainsMediaTypeParameters(MEDIA_TYPE);
    }

    private bool UsingSplitImpl(string mediaType)
    {
        var mediaTypeArr = mediaType.Split(';');
        return mediaTypeArr[0] == Constants.ContentType && mediaTypeArr.Length == 2;
    }

    #endregion
}
