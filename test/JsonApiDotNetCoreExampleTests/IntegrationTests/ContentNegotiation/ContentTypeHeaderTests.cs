using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using JsonApiDotNetCore.Middleware;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreExampleTests.Startups;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreExampleTests.IntegrationTests.ContentNegotiation
{
    public sealed class ContentTypeHeaderTests 
        : IClassFixture<ExampleIntegrationTestContext<TestableStartup<PolicyDbContext>, PolicyDbContext>>
    {
        private readonly ExampleIntegrationTestContext<TestableStartup<PolicyDbContext>, PolicyDbContext> _testContext;

        public ContentTypeHeaderTests(ExampleIntegrationTestContext<TestableStartup<PolicyDbContext>, PolicyDbContext> testContext)
        {
            _testContext = testContext;

            testContext.ConfigureServicesAfterStartup(services => services.AddControllersFromExampleProject());
        }

        [Fact]
        public async Task Returns_JsonApi_ContentType_header()
        {
            // Arrange
            const string route = "/policies";

            // Act
            var (httpResponse, _) = await _testContext.ExecuteGetAsync<Document>(route);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);
            httpResponse.Content.Headers.ContentType.ToString().Should().Be(HeaderConstants.MediaType);
        }

        [Fact]
        public async Task Returns_JsonApi_ContentType_header_with_AtomicOperations_extension()
        {
            // Arrange
            var requestBody = new
            {
                atomic__operations = new[]
                {
                    new
                    {
                        op = "add",
                        data = new
                        {
                            type = "policies",
                            attributes = new
                            {
                                name = "some"
                            }
                        }
                    }
                }
            };

            const string route = "/operations";

            // Act
            var (httpResponse, _) = await _testContext.ExecutePostAtomicAsync<Document>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);
            httpResponse.Content.Headers.ContentType.ToString().Should().Be(HeaderConstants.AtomicOperationsMediaType);
        }

        [Fact]
        public async Task Denies_unknown_ContentType_header()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = "text/html";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be("Please specify 'application/vnd.api+json' instead of 'text/html' for the Content-Type header value.");
        }

        [Fact]
        public async Task Permits_JsonApi_ContentType_header()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.MediaType;

            // Act
            // ReSharper disable once RedundantArgumentDefaultValue
            var (httpResponse, _) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.Created);
        }

        [Fact]
        public async Task Permits_JsonApi_ContentType_header_with_AtomicOperations_extension_at_operations_endpoint()
        {
            // Arrange
            var requestBody = new
            {
                atomic__operations = new[]
                {
                    new
                    {
                        op = "add",
                        data = new
                        {
                            type = "policies",
                            attributes = new
                            {
                                name = "some"
                            }
                        }
                    }
                }
            };

            const string route = "/operations";
            const string contentType = HeaderConstants.AtomicOperationsMediaType;

            // Act
            var (httpResponse, _) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_with_profile()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.MediaType + "; profile=something";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be($"Please specify 'application/vnd.api+json' instead of '{contentType}' for the Content-Type header value.");
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_with_extension()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.MediaType + "; ext=something";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be($"Please specify 'application/vnd.api+json' instead of '{contentType}' for the Content-Type header value.");
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_with_AtomicOperations_extension_at_resource_endpoint()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.AtomicOperationsMediaType;

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be($"Please specify 'application/vnd.api+json' instead of '{contentType}' for the Content-Type header value.");
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_with_CharSet()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.MediaType + "; charset=ISO-8859-4";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be($"Please specify 'application/vnd.api+json' instead of '{contentType}' for the Content-Type header value.");
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_with_unknown_parameter()
        {
            // Arrange
            var requestBody = new
            {
                data = new
                {
                    type = "policies",
                    attributes = new
                    {
                        name = "some"
                    }
                }
            };

            const string route = "/policies";
            const string contentType = HeaderConstants.MediaType + "; unknown=unexpected";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be($"Please specify 'application/vnd.api+json' instead of '{contentType}' for the Content-Type header value.");
        }

        [Fact]
        public async Task Denies_JsonApi_ContentType_header_at_operations_endpoint()
        {
            // Arrange
            var requestBody = new
            {
                atomic__operations = new[]
                {
                    new
                    {
                        op = "add",
                        data = new
                        {
                            type = "policies",
                            attributes = new
                            {
                                name = "some"
                            }
                        }
                    }
                }
            };

            const string route = "/operations";
            const string contentType = HeaderConstants.MediaType;

            // Act
            // ReSharper disable once RedundantArgumentDefaultValue
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody, contentType);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnsupportedMediaType);

            responseDocument.Errors.Should().HaveCount(1);

            string detail = $"Please specify '{HeaderConstants.AtomicOperationsMediaType}' instead of '{contentType}' for the Content-Type header value.";

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
            error.Title.Should().Be("The specified Content-Type header value is not supported.");
            error.Detail.Should().Be(detail);
        }
    }
}
