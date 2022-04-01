using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using JsonApiDotNetCore.Builders;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Internal.Generics;
using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Serialization;
using JsonApiDotNetCore.Services;

using Moq;

using Newtonsoft.Json.Serialization;

namespace Benchmarks.Serialization;

[MarkdownExporter]
public class JsonApiSerializer_Benchmarks
{
    #region Static Fields and Constants

    private const string TYPE_NAME = "simple-types";
    private static readonly SimpleType Content = new();

    #endregion

    #region Constructors

    public JsonApiSerializer_Benchmarks()
    {
        var resourceGraphBuilder = new ResourceGraphBuilder();
        resourceGraphBuilder.AddResource<SimpleType>(TYPE_NAME);
        var resourceGraph = resourceGraphBuilder.Build();

        var jsonApiContextMock = new Mock<IJsonApiContext>();
        jsonApiContextMock.SetupAllProperties();
        jsonApiContextMock.Setup(m => m.ResourceGraph).Returns(resourceGraph);
        jsonApiContextMock.Setup(m => m.AttributesToUpdate).Returns(new Dictionary<AttrAttribute, object>());

        var jsonApiOptions = new JsonApiOptions();
        jsonApiOptions.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        jsonApiContextMock.Setup(m => m.Options).Returns(jsonApiOptions);

        var genericProcessorFactoryMock = new Mock<IGenericProcessorFactory>();

        var documentBuilder = new DocumentBuilder(jsonApiContextMock.Object);
        _jsonApiSerializer = new JsonApiSerializer(jsonApiContextMock.Object, documentBuilder);
    }

    #endregion

    #region Fields

    private readonly JsonApiSerializer _jsonApiSerializer;

    #endregion

    #region Methods

    [Benchmark]
    public object SerializeSimpleObject()
    {
        return _jsonApiSerializer.Serialize(Content);
    }

    #endregion

    #region Nested type: SimpleType

    private class SimpleType : Identifiable
    {
        #region Properties

        [Attr("name")] public string Name { get; set; }

        #endregion
    }

    #endregion
}
