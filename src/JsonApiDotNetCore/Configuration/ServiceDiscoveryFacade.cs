using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JsonApiDotNetCore.Errors;
using JsonApiDotNetCore.Repositories;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCore.Configuration
{
    /// <summary>
    /// Scans for types like resources, services, repositories and resource definitions in an assembly and registers them to the IoC container.
    /// </summary>
    [PublicAPI]
    public class ServiceDiscoveryFacade
    {
        internal static readonly HashSet<Type> ServiceInterfaces = new HashSet<Type> {
            typeof(IResourceService<>),
            typeof(IResourceService<,>),
            typeof(IResourceCommandService<>),
            typeof(IResourceCommandService<,>),
            typeof(IResourceQueryService<>),
            typeof(IResourceQueryService<,>),
            typeof(IGetAllService<>),
            typeof(IGetAllService<,>),
            typeof(IGetByIdService<>),
            typeof(IGetByIdService<,>),
            typeof(IGetSecondaryService<>),
            typeof(IGetSecondaryService<,>),
            typeof(IGetRelationshipService<>),
            typeof(IGetRelationshipService<,>),
            typeof(ICreateService<>),
            typeof(ICreateService<,>),
            typeof(IAddToRelationshipService<>),
            typeof(IAddToRelationshipService<,>),
            typeof(IUpdateService<>),
            typeof(IUpdateService<,>),
            typeof(ISetRelationshipService<>),
            typeof(ISetRelationshipService<,>),
            typeof(IDeleteService<>),
            typeof(IDeleteService<,>),
            typeof(IRemoveFromRelationshipService<>),
            typeof(IRemoveFromRelationshipService<,>)
        };

        internal static readonly HashSet<Type> RepositoryInterfaces = new HashSet<Type> {
            typeof(IResourceRepository<>),
            typeof(IResourceRepository<,>),
            typeof(IResourceWriteRepository<>),
            typeof(IResourceWriteRepository<,>),
            typeof(IResourceReadRepository<>),
            typeof(IResourceReadRepository<,>)
        };

        internal static readonly HashSet<Type> ResourceDefinitionInterfaces = new HashSet<Type> {
            typeof(IResourceDefinition<>),
            typeof(IResourceDefinition<,>)
        };

        private readonly ILogger<ServiceDiscoveryFacade> _logger;
        private readonly IServiceCollection _services;
        private readonly ResourceGraphBuilder _resourceGraphBuilder;
        private readonly IJsonApiOptions _options;
        private readonly ResourceDescriptorAssemblyCache _assemblyCache = new ResourceDescriptorAssemblyCache();

        public ServiceDiscoveryFacade(IServiceCollection services, ResourceGraphBuilder resourceGraphBuilder, IJsonApiOptions options, ILoggerFactory loggerFactory)
        {
            ArgumentGuard.NotNull(services, nameof(services));
            ArgumentGuard.NotNull(resourceGraphBuilder, nameof(resourceGraphBuilder));
            ArgumentGuard.NotNull(loggerFactory, nameof(loggerFactory));
            ArgumentGuard.NotNull(options, nameof(options));

            _logger = loggerFactory.CreateLogger<ServiceDiscoveryFacade>();
            _services = services;
            _resourceGraphBuilder = resourceGraphBuilder;
            _options = options;
        }

        /// <summary>
        /// Mark the calling assembly for scanning of resources and injectables.
        /// </summary>
        public ServiceDiscoveryFacade AddCurrentAssembly() => AddAssembly(Assembly.GetCallingAssembly());

        /// <summary>
        /// Mark the specified assembly for scanning of resources and injectables.
        /// </summary>
        public ServiceDiscoveryFacade AddAssembly(Assembly assembly)
        {
            ArgumentGuard.NotNull(assembly, nameof(assembly));

            _assemblyCache.RegisterAssembly(assembly);
            _logger.LogDebug($"Registering assembly '{assembly.FullName}' for discovery of resources and injectables.");

            return this;
        }

        internal void DiscoverResources()
        {
            foreach (var resourceDescriptor in _assemblyCache.GetResourceDescriptorsPerAssembly().SelectMany(tuple => tuple.resourceDescriptors))
            {
                AddResource(resourceDescriptor);
            }
        }

        internal void DiscoverInjectables()
        {
            foreach (var (assembly, resourceDescriptors) in _assemblyCache.GetResourceDescriptorsPerAssembly())
            {
                AddDbContextResolvers(assembly);
                AddInjectables(resourceDescriptors, assembly);
            }
        }

        private void AddInjectables(IReadOnlyCollection<ResourceDescriptor> resourceDescriptors, Assembly assembly)
        {
            foreach (var resourceDescriptor in resourceDescriptors)
            {
                AddServices(assembly, resourceDescriptor);
                AddRepositories(assembly, resourceDescriptor);
                AddResourceDefinitions(assembly, resourceDescriptor);

                if (_options.EnableResourceHooks)
                {
                    AddResourceHookDefinitions(assembly, resourceDescriptor);
                }
            }
        }

        private void AddDbContextResolvers(Assembly assembly)
        {
            var dbContextTypes = TypeLocator.GetDerivedTypes(assembly, typeof(DbContext));
            foreach (var dbContextType in dbContextTypes)
            {
                var resolverType = typeof(DbContextResolver<>).MakeGenericType(dbContextType);
                _services.AddScoped(typeof(IDbContextResolver), resolverType);
            }
        }
        
        private void AddResource(ResourceDescriptor resourceDescriptor)
        {
            _resourceGraphBuilder.Add(resourceDescriptor.ResourceType, resourceDescriptor.IdType);
        }

        private void AddResourceHookDefinitions(Assembly assembly, ResourceDescriptor identifiable)
        {
            try
            {
                var resourceDefinition = TypeLocator.GetDerivedGenericTypes(assembly, typeof(ResourceHooksDefinition<>), identifiable.ResourceType)
                    .SingleOrDefault();

                if (resourceDefinition != null)
                {
                    _services.AddScoped(typeof(ResourceHooksDefinition<>).MakeGenericType(identifiable.ResourceType), resourceDefinition);
                }
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidConfigurationException($"Cannot define multiple ResourceHooksDefinition<> implementations for '{identifiable.ResourceType}'", e);
            }
        }

        private void AddServices(Assembly assembly, ResourceDescriptor resourceDescriptor)
        {
            foreach (var serviceInterface in ServiceInterfaces)
            {
                RegisterImplementations(assembly, serviceInterface, resourceDescriptor);
            }
        }

        private void AddRepositories(Assembly assembly, ResourceDescriptor resourceDescriptor)
        {
            foreach (var repositoryInterface in RepositoryInterfaces)
            {
                RegisterImplementations(assembly, repositoryInterface, resourceDescriptor);
            }
        }
        
        private void AddResourceDefinitions(Assembly assembly, ResourceDescriptor resourceDescriptor)
        {
            foreach (var resourceDefinitionInterface in ResourceDefinitionInterfaces)
            {
                RegisterImplementations(assembly, resourceDefinitionInterface, resourceDescriptor);
            }
        }

        private void RegisterImplementations(Assembly assembly, Type interfaceType, ResourceDescriptor resourceDescriptor)
        {
            var genericArguments = interfaceType.GetTypeInfo().GenericTypeParameters.Length == 2
                ? ArrayFactory.Create(resourceDescriptor.ResourceType, resourceDescriptor.IdType)
                : ArrayFactory.Create(resourceDescriptor.ResourceType);

            var result = TypeLocator.GetGenericInterfaceImplementation(assembly, interfaceType, genericArguments);
            if (result != null)
            {
                var (implementation, registrationInterface) = result.Value;
                _services.AddScoped(registrationInterface, implementation);
            }
        }
    }
}
