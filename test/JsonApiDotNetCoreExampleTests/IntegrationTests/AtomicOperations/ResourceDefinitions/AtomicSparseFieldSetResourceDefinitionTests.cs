using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreExampleTests.Startups;
using Microsoft.Extensions.DependencyInjection;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreExampleTests.IntegrationTests.AtomicOperations.ResourceDefinitions
{
    public sealed class AtomicSparseFieldSetResourceDefinitionTests
        : IClassFixture<ExampleIntegrationTestContext<TestableStartup<OperationsDbContext>, OperationsDbContext>>
    {
        private readonly ExampleIntegrationTestContext<TestableStartup<OperationsDbContext>, OperationsDbContext> _testContext;
        private readonly OperationsFakers _fakers = new OperationsFakers();

        public AtomicSparseFieldSetResourceDefinitionTests(ExampleIntegrationTestContext<TestableStartup<OperationsDbContext>, OperationsDbContext> testContext)
        {
            _testContext = testContext;

            testContext.ConfigureServicesAfterStartup(services =>
            {
                services.AddControllersFromExampleProject();

                services.AddSingleton<LyricPermissionProvider>();
                services.AddScoped<IResourceDefinition<Lyric, long>, LyricTextDefinition>();
                services.AddScoped(typeof(IResourceChangeTracker<>), typeof(NeverSameResourceChangeTracker<>));
            });
        }

        [Fact]
        public async Task Hides_text_in_create_resource_with_side_effects()
        {
            // Arrange
            var provider = _testContext.Factory.Services.GetRequiredService<LyricPermissionProvider>();
            provider.CanViewText = false;
            provider.HitCount = 0;

            var newLyrics = _fakers.Lyric.Generate(2);

            var requestBody = new
            {
                atomic__operations = new[]
                {
                    new
                    {
                        op = "add",
                        data = new
                        {
                            type = "lyrics",
                            attributes = new
                            {
                                format = newLyrics[0].Format,
                                text = newLyrics[0].Text
                            }
                        }
                    },
                    new
                    {
                        op = "add",
                        data = new
                        {
                            type = "lyrics",
                            attributes = new
                            {
                                format = newLyrics[1].Format,
                                text = newLyrics[1].Text
                            }
                        }
                    }
                }
            };

            const string route = "/operations";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAtomicAsync<AtomicOperationsDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.Results.Should().HaveCount(2);

            responseDocument.Results[0].SingleData.Attributes["format"].Should().Be(newLyrics[0].Format);
            responseDocument.Results[0].SingleData.Attributes.Should().NotContainKey("text");

            responseDocument.Results[1].SingleData.Attributes["format"].Should().Be(newLyrics[1].Format);
            responseDocument.Results[1].SingleData.Attributes.Should().NotContainKey("text");

            provider.HitCount.Should().Be(4);
        }

        [Fact]
        public async Task Hides_text_in_update_resource_with_side_effects()
        {
            // Arrange
            var provider = _testContext.Factory.Services.GetRequiredService<LyricPermissionProvider>();
            provider.CanViewText = false;
            provider.HitCount = 0;

            var existingLyrics = _fakers.Lyric.Generate(2);

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.Lyrics.AddRange(existingLyrics);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                atomic__operations = new[]
                {
                    new
                    {
                        op = "update",
                        data = new
                        {
                            type = "lyrics",
                            id = existingLyrics[0].StringId,
                            attributes = new
                            {
                            }
                        }
                    },
                    new
                    {
                        op = "update",
                        data = new
                        {
                            type = "lyrics",
                            id = existingLyrics[1].StringId,
                            attributes = new
                            {
                            }
                        }
                    }
                }
            };

            const string route = "/operations";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAtomicAsync<AtomicOperationsDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.Results.Should().HaveCount(2);

            responseDocument.Results[0].SingleData.Attributes["format"].Should().Be(existingLyrics[0].Format);
            responseDocument.Results[0].SingleData.Attributes.Should().NotContainKey("text");

            responseDocument.Results[1].SingleData.Attributes["format"].Should().Be(existingLyrics[1].Format);
            responseDocument.Results[1].SingleData.Attributes.Should().NotContainKey("text");

            provider.HitCount.Should().Be(4);
        }
    }
}
