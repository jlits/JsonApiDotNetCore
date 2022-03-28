using System;
using System.Linq;

using JsonApiDotNetCore.Internal;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JsonApiDotNetCore.Extensions
{
    public static class ModelStateExtensions
    {
        #region Methods

        [Obsolete(
            "Use Generic Method ConvertToErrorCollection<T>(IResourceGraph resourceGraph) instead for full validation errors")]
        public static ErrorCollection ConvertToErrorCollection(this ModelStateDictionary modelState)
        {
            var collection = new ErrorCollection();
            foreach (var entry in modelState)
            {
                if (entry.Value.Errors.Any() == false)
                    continue;

                foreach (var modelError in entry.Value.Errors)
                    if (modelError.Exception is JsonApiException jex)
                        collection.Errors.AddRange(jex.GetError().Errors);
                    else
                        collection.Errors.Add(new Error(400, entry.Key, modelError.ErrorMessage,
                            modelError.Exception != null ? ErrorMeta.FromException(modelError.Exception) : null));
            }

            return collection;
        }

        public static ErrorCollection ConvertToErrorCollection<T>(this ModelStateDictionary modelState,
            IResourceGraph resourceGraph)
        {
            var collection = new ErrorCollection();
            foreach (var entry in modelState)
            {
                if (entry.Value.Errors.Any() == false)
                    continue;

                var attrName = resourceGraph.GetPublicAttributeName<T>(entry.Key);

                foreach (var modelError in entry.Value.Errors)
                    if (modelError.Exception is JsonApiException jex)
                        collection.Errors.AddRange(jex.GetError().Errors);
                    else
                        collection.Errors.Add(new Error(
                            422,
                            entry.Key,
                            modelError.ErrorMessage,
                            modelError.Exception != null ? ErrorMeta.FromException(modelError.Exception) : null,
                            attrName == null
                                ? null
                                : new
                                {
                                    pointer = $"/data/attributes/{attrName}"
                                }));
            }

            return collection;
        }

        #endregion
    }
}
