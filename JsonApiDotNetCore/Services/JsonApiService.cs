using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using JsonApiDotNetCore.Abstractions;
using JsonApiDotNetCore.Attributes;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Controllers;
using JsonApiDotNetCore.JsonApi;
using JsonApiDotNetCore.Extensions;

namespace JsonApiDotNetCore.Services
{
  public class JsonApiService
  {
    private readonly JsonApiModelConfiguration _jsonApiModelConfiguration;
    private IServiceProvider _serviceProvider;

    public JsonApiService(JsonApiModelConfiguration configuration)
    {
      _jsonApiModelConfiguration = configuration;
    }

    public bool HandleJsonApiRoute(HttpContext context, IServiceProvider serviceProvider)
    {
      _serviceProvider = serviceProvider;

      var route = context.Request.Path;
      var requestMethod = context.Request.Method;
      var controllerMethodIdentifier = _jsonApiModelConfiguration.GetControllerMethodIdentifierForRoute(route, requestMethod);
      if (controllerMethodIdentifier == null) return false;
      CallControllerMethod(controllerMethodIdentifier, context);
      return true;
    }

    private void CallControllerMethod(ControllerMethodIdentifier controllerMethodIdentifier, HttpContext context)
    {
      var dbContext = _serviceProvider.GetService(_jsonApiModelConfiguration.ContextType);
      var jsonApiContext = new JsonApiContext(controllerMethodIdentifier.Route, dbContext);
      var controller = new JsonApiController(context, jsonApiContext);
      var resourceId = controllerMethodIdentifier.GetResourceId();
      switch (controllerMethodIdentifier.RequestMethod)
      {
        case "GET":
          if (string.IsNullOrEmpty(resourceId))
          {
            var result = controller.Get();
            result.Value = SerializeResponse(jsonApiContext, result.Value);
            SendResponse(context, result);
          }
          else
          {
            controller.Get(resourceId);
          }
          break;
        case "POST":
          controller.Post(null); // TODO: need the request body
          break;
        case "PUT":
          controller.Put(resourceId, null);
          break;
        case "DELETE":
          controller.Delete(resourceId);
          break;
        default:
          throw new ArgumentException("Request method not supported", nameof(controllerMethodIdentifier));
      }
    }

    private string SerializeResponse(JsonApiContext context, object resultValue)
    {
      var response = new JsonApiDocument
      {
        Data = GetJsonApiDocumentData(context, resultValue)
      };

      return JsonConvert.SerializeObject(response, new JsonSerializerSettings
      {
          ContractResolver = new CamelCasePropertyNamesContractResolver()
      });
    }

    private object GetJsonApiDocumentData(JsonApiContext context, object result)
    {
      var enumerableResult = result as IEnumerable;

      if (enumerableResult == null) return ResourceToJsonApiDatum(context, EntityToJsonApiResource(result));

      var data = new List<JsonApiDatum>();
      foreach (var resource in enumerableResult)
      {
        data.Add(ResourceToJsonApiDatum(context, EntityToJsonApiResource(resource)));
      }
      return data;
    }

    private IJsonApiResource EntityToJsonApiResource(object entity)
    {
      var resource = entity as IJsonApiResource;
      if (resource != null) return resource;

      var attributes = TypeDescriptor.GetAttributes(entity);
      var type = ((JsonApiResourceAttribute)attributes[typeof(JsonApiResourceAttribute)]).JsonApiResourceType;
      return (IJsonApiResource)_jsonApiModelConfiguration.ResourceMaps.Map(entity, entity.GetType(), type);
    }

    private static JsonApiDatum ResourceToJsonApiDatum(JsonApiContext context, IJsonApiResource resource)
    {
      return new JsonApiDatum
      {
        Type = context.Route.ContextPropertyName.ToCamelCase(),
        Id = resource.Id,
        Attributes = GetAttributesFromResource(resource)
      };
    }

    private static Dictionary<string, object> GetAttributesFromResource(IJsonApiResource resource)
    {
      return resource.GetType().GetProperties()
        .Where(propertyInfo => propertyInfo.GetMethod.IsVirtual == false)
        .ToDictionary(
          propertyInfo => propertyInfo.Name, propertyInfo => propertyInfo.GetValue(resource)
        );
    }

    private void SendResponse(HttpContext context, ObjectResult result)
    {
      context.Response.StatusCode = result.StatusCode ?? 500;
      context.Response.WriteAsync(result.Value.ToString());
      context.Response.Body.Flush();
    }
  }
}