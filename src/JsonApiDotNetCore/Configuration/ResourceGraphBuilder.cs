using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCore.Configuration
{
    /// <summary>
    /// Builds and configures the <see cref="ResourceGraph"/>.
    /// </summary>
    [PublicAPI]
    public class ResourceGraphBuilder
    {
        private readonly IJsonApiOptions _options;
        private readonly ILogger<ResourceGraphBuilder> _logger;
        private readonly List<ResourceContext> _resources = new List<ResourceContext>();

        public ResourceGraphBuilder(IJsonApiOptions options, ILoggerFactory loggerFactory)
        {
            ArgumentGuard.NotNull(options, nameof(options));
            ArgumentGuard.NotNull(loggerFactory, nameof(loggerFactory));

            _options = options;
            _logger = loggerFactory.CreateLogger<ResourceGraphBuilder>();
        }

        /// <summary>
        /// Constructs the <see cref="ResourceGraph"/>.
        /// </summary>
        public IResourceGraph Build()
        {
            _resources.ForEach(SetResourceLinksOptions);
            return new ResourceGraph(_resources);
        }

        private void SetResourceLinksOptions(ResourceContext resourceContext)
        {
            var attribute = (ResourceLinksAttribute)resourceContext.ResourceType.GetCustomAttribute(typeof(ResourceLinksAttribute));
            if (attribute != null)
            {
                resourceContext.RelationshipLinks = attribute.RelationshipLinks;
                resourceContext.ResourceLinks = attribute.ResourceLinks;
                resourceContext.TopLevelLinks = attribute.TopLevelLinks;
            }
        }
        
        /// <summary>
        /// Adds a JSON:API resource with <code>int</code> as the identifier type.
        /// </summary>
        /// <typeparam name="TResource">The resource model type.</typeparam>
        /// <param name="publicName">
        /// The name under which the resource is publicly exposed by the API. 
        /// If nothing is specified, the configured naming convention formatter will be applied.
        /// </param>
        public ResourceGraphBuilder Add<TResource>(string publicName = null) where TResource : class, IIdentifiable<int>
            => Add<TResource, int>(publicName);
        
        /// <summary>
        /// Adds a JSON:API resource.
        /// </summary>
        /// <typeparam name="TResource">The resource model type.</typeparam>
        /// <typeparam name="TId">The resource model identifier type.</typeparam>
        /// <param name="publicName">
        /// The name under which the resource is publicly exposed by the API. 
        /// If nothing is specified, the configured naming convention formatter will be applied.
        /// </param>
        public ResourceGraphBuilder Add<TResource, TId>(string publicName = null) where TResource : class, IIdentifiable<TId>
            => Add(typeof(TResource), typeof(TId), publicName);
        
        /// <summary>
        /// Adds a JSON:API resource.
        /// </summary>
        /// <param name="resourceType">The resource model type.</param>
        /// <param name="idType">The resource model identifier type.</param>
        /// <param name="publicName">
        /// The name under which the resource is publicly exposed by the API. 
        /// If nothing is specified, the configured naming convention formatter will be applied.
        /// </param>
        public ResourceGraphBuilder Add(Type resourceType, Type idType = null, string publicName = null)
        {
            ArgumentGuard.NotNull(resourceType, nameof(resourceType));

            if (_resources.Any(e => e.ResourceType == resourceType))
            {
                return this;
            }

            if (TypeHelper.IsOrImplementsInterface(resourceType, typeof(IIdentifiable)))
            {
                var effectivePublicName = publicName ?? FormatResourceName(resourceType);
                var effectiveIdType = idType ?? TypeLocator.TryGetIdType(resourceType);
                
                var resourceContext = CreateResourceContext(effectivePublicName, resourceType, effectiveIdType);
                _resources.Add(resourceContext);
            }
            else
            {
                _logger.LogWarning($"Entity '{resourceType}' does not implement '{nameof(IIdentifiable)}'.");
            }

            return this;
        }

        private ResourceContext CreateResourceContext(string publicName, Type resourceType, Type idType) => new ResourceContext
        {
            PublicName = publicName,
            ResourceType = resourceType,
            IdentityType = idType,
            Attributes = GetAttributes(resourceType),
            Relationships = GetRelationships(resourceType),
            EagerLoads = GetEagerLoads(resourceType)
        };

        private IReadOnlyCollection<AttrAttribute> GetAttributes(Type resourceType)
        {
            ArgumentGuard.NotNull(resourceType, nameof(resourceType));

            var attributes = new List<AttrAttribute>();

            foreach (var property in resourceType.GetProperties())
            {
                var attribute = (AttrAttribute)property.GetCustomAttribute(typeof(AttrAttribute));

                // Although strictly not correct, 'id' is added to the list of attributes for convenience.
                // For example, it enables to filter on ID, without the need to special-case existing logic.
                // And when using sparse fields, it silently adds 'id' to the set of attributes to retrieve.
                if (property.Name == nameof(Identifiable.Id) && attribute == null)
                {
                    var idAttr = new AttrAttribute
                    {
                        PublicName = FormatPropertyName(property),
                        Property = property,
                        Capabilities = _options.DefaultAttrCapabilities
                    };
                    attributes.Add(idAttr);
                    continue;
                }

                if (attribute == null)
                {
                    continue;
                }

                attribute.PublicName ??= FormatPropertyName(property);
                attribute.Property = property;

                if (!attribute.HasExplicitCapabilities)
                {
                    attribute.Capabilities = _options.DefaultAttrCapabilities;
                }

                attributes.Add(attribute);
            }
            return attributes;
        }

        private IReadOnlyCollection<RelationshipAttribute> GetRelationships(Type resourceType)
        {
            ArgumentGuard.NotNull(resourceType, nameof(resourceType));

            var attributes = new List<RelationshipAttribute>();
            var properties = resourceType.GetProperties();
            foreach (var prop in properties)
            {
                var attribute = (RelationshipAttribute)prop.GetCustomAttribute(typeof(RelationshipAttribute));
                if (attribute == null)
                {
                    continue;
                }

                attribute.Property = prop;
                attribute.PublicName ??= FormatPropertyName(prop);
                attribute.RightType = GetRelationshipType(attribute, prop);
                attribute.LeftType = resourceType;
                attributes.Add(attribute);

                if (attribute is HasManyThroughAttribute hasManyThroughAttribute)
                {
                    var throughProperty = properties.SingleOrDefault(p => p.Name == hasManyThroughAttribute.ThroughPropertyName);
                    if (throughProperty == null)
                    {
                        throw new InvalidConfigurationException($"Invalid {nameof(HasManyThroughAttribute)} on '{resourceType}.{attribute.Property.Name}': " +
                            $"Resource does not contain a property named '{hasManyThroughAttribute.ThroughPropertyName}'.");
                    }

                    var throughType = TryGetThroughType(throughProperty);
                    if (throughType == null)
                    {
                        throw new InvalidConfigurationException($"Invalid {nameof(HasManyThroughAttribute)} on '{resourceType}.{attribute.Property.Name}': " +
                            $"Referenced property '{throughProperty.Name}' does not implement 'ICollection<T>'.");
                    }

                    // ICollection<ArticleTag>
                    hasManyThroughAttribute.ThroughProperty = throughProperty;

                    // ArticleTag
                    hasManyThroughAttribute.ThroughType = throughType;

                    var throughProperties = throughType.GetProperties();

                    // ArticleTag.Article
                    if (hasManyThroughAttribute.LeftPropertyName != null)
                    {
                        // In case of a self-referencing many-to-many relationship, the left property name must be specified.
                        hasManyThroughAttribute.LeftProperty = hasManyThroughAttribute.ThroughType.GetProperty(hasManyThroughAttribute.LeftPropertyName)
                            ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a navigation property named '{hasManyThroughAttribute.LeftPropertyName}'.");
                    }
                    else
                    {
                        // In case of a non-self-referencing many-to-many relationship, we just pick the single compatible type.
                        hasManyThroughAttribute.LeftProperty = throughProperties.SingleOrDefault(x => x.PropertyType.IsAssignableFrom(resourceType)) 
                            ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a navigation property to type '{resourceType}'.");
                    }

                    // ArticleTag.ArticleId
                    var leftIdPropertyName = hasManyThroughAttribute.LeftIdPropertyName ?? hasManyThroughAttribute.LeftProperty.Name + "Id";
                    hasManyThroughAttribute.LeftIdProperty = throughProperties.SingleOrDefault(x => x.Name == leftIdPropertyName)
                        ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a relationship ID property to type '{resourceType}' with name '{leftIdPropertyName}'.");

                    // ArticleTag.Tag
                    if (hasManyThroughAttribute.RightPropertyName != null)
                    {
                        // In case of a self-referencing many-to-many relationship, the right property name must be specified.
                        hasManyThroughAttribute.RightProperty = hasManyThroughAttribute.ThroughType.GetProperty(hasManyThroughAttribute.RightPropertyName)
                            ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a navigation property named '{hasManyThroughAttribute.RightPropertyName}'.");
                    }
                    else
                    {
                        // In case of a non-self-referencing many-to-many relationship, we just pick the single compatible type.
                        hasManyThroughAttribute.RightProperty = throughProperties.SingleOrDefault(x => x.PropertyType == hasManyThroughAttribute.RightType)
                            ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a navigation property to type '{hasManyThroughAttribute.RightType}'.");
                    }

                    // ArticleTag.TagId
                    var rightIdPropertyName = hasManyThroughAttribute.RightIdPropertyName ?? hasManyThroughAttribute.RightProperty.Name + "Id";
                    hasManyThroughAttribute.RightIdProperty = throughProperties.SingleOrDefault(x => x.Name == rightIdPropertyName)
                        ?? throw new InvalidConfigurationException($"'{throughType}' does not contain a relationship ID property to type '{hasManyThroughAttribute.RightType}' with name '{rightIdPropertyName}'.");
                }
            }

            return attributes;
        }

        private Type TryGetThroughType(PropertyInfo throughProperty)
        {
            if (throughProperty.PropertyType.IsGenericType)
            {
                var typeArguments = throughProperty.PropertyType.GetGenericArguments();
                if (typeArguments.Length == 1)
                {
                    var constructedThroughType = typeof(ICollection<>).MakeGenericType(typeArguments[0]);
                    if (TypeHelper.IsOrImplementsInterface(throughProperty.PropertyType, constructedThroughType))
                    {
                        return typeArguments[0];
                    }
                }
            }

            return null;
        }

        private Type GetRelationshipType(RelationshipAttribute relationship, PropertyInfo property)
        {
            ArgumentGuard.NotNull(relationship, nameof(relationship));
            ArgumentGuard.NotNull(property, nameof(property));

            return relationship is HasOneAttribute ? property.PropertyType : property.PropertyType.GetGenericArguments()[0];
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private IReadOnlyCollection<EagerLoadAttribute> GetEagerLoads(Type resourceType, int recursionDepth = 0)
        {
            if (recursionDepth >= 500)
            {
                throw new InvalidOperationException("Infinite recursion detected in eager-load chain.");
            }

            var attributes = new List<EagerLoadAttribute>();
            var properties = resourceType.GetProperties();

            foreach (var property in properties)
            {
                var attribute = (EagerLoadAttribute) property.GetCustomAttribute(typeof(EagerLoadAttribute));
                if (attribute == null)
                {
                    continue;
                }

                Type innerType = TypeOrElementType(property.PropertyType);
                attribute.Children = GetEagerLoads(innerType, recursionDepth + 1);
                attribute.Property = property;

                attributes.Add(attribute);
            }

            return attributes;
        }

        private Type TypeOrElementType(Type type)
        {
            var interfaces = type.GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();

            return interfaces.Length == 1 ? interfaces.Single().GenericTypeArguments[0] : type;
        }

        private string FormatResourceName(Type resourceType)
        {
            var formatter = new ResourceNameFormatter(_options.SerializerNamingStrategy);
            return formatter.FormatResourceName(resourceType);
        }

        private string FormatPropertyName(PropertyInfo resourceProperty)
        {
            return _options.SerializerNamingStrategy.GetPropertyName(resourceProperty.Name, false);
        }
    }
}
