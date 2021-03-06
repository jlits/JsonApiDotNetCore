using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using JsonApiDotNetCore.Services;

namespace JsonApiDotNetCore.AtomicOperations.Processors
{
    /// <inheritdoc />
    [PublicAPI]
    public class SetRelationshipProcessor<TResource, TId> : ISetRelationshipProcessor<TResource, TId>
        where TResource : class, IIdentifiable<TId>
    {
        private readonly ISetRelationshipService<TResource, TId> _service;

        public SetRelationshipProcessor(ISetRelationshipService<TResource, TId> service)
        {
            ArgumentGuard.NotNull(service, nameof(service));

            _service = service;
        }

        /// <inheritdoc />
        public virtual async Task<OperationContainer> ProcessAsync(OperationContainer operation,
            CancellationToken cancellationToken)
        {
            ArgumentGuard.NotNull(operation, nameof(operation));

            var primaryId = (TId) operation.Resource.GetTypedId();
            object rightValue = GetRelationshipRightValue(operation);

            await _service.SetRelationshipAsync(primaryId, operation.Request.Relationship.PublicName, rightValue,
                cancellationToken);

            return null;
        }

        private static object GetRelationshipRightValue(OperationContainer operation)
        {
            var relationship = operation.Request.Relationship;
            var rightValue = relationship.GetValue(operation.Resource);

            if (relationship is HasManyAttribute)
            {
                var rightResources = TypeHelper.ExtractResources(rightValue);
                return rightResources.ToHashSet(IdentifiableComparer.Instance);
            }

            return rightValue;
        }
    }
}
