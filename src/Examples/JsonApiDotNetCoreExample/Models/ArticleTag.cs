using JetBrains.Annotations;

namespace JsonApiDotNetCoreExample.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    public sealed class ArticleTag
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }
}
