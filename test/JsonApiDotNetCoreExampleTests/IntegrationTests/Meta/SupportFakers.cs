using System;
using Bogus;
using TestBuildingBlocks;

// @formatter:wrap_chained_method_calls chop_always
// @formatter:keep_existing_linebreaks true

namespace JsonApiDotNetCoreExampleTests.IntegrationTests.Meta
{
    internal sealed class SupportFakers : FakerContainer
    {
        private readonly Lazy<Faker<ProductFamily>> _lazyProductFamilyFaker = new Lazy<Faker<ProductFamily>>(() =>
            new Faker<ProductFamily>()
                .UseSeed(GetFakerSeed())
                .RuleFor(productFamily => productFamily.Name, f => f.Commerce.ProductName()));

        private readonly Lazy<Faker<SupportTicket>> _lazySupportTicketFaker = new Lazy<Faker<SupportTicket>>(() =>
            new Faker<SupportTicket>()
                .UseSeed(GetFakerSeed())
                .RuleFor(supportTicket => supportTicket.Description, f => f.Lorem.Paragraph()));

        public Faker<ProductFamily> ProductFamily => _lazyProductFamilyFaker.Value;
        public Faker<SupportTicket> SupportTicket => _lazySupportTicketFaker.Value;
    }
}
