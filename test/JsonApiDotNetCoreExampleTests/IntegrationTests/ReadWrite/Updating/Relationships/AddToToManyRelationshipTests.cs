using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreExampleTests.Startups;
using Microsoft.EntityFrameworkCore;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreExampleTests.IntegrationTests.ReadWrite.Updating.Relationships
{
    public sealed class AddToToManyRelationshipTests
        : IClassFixture<ExampleIntegrationTestContext<TestableStartup<ReadWriteDbContext>, ReadWriteDbContext>>
    {
        private readonly ExampleIntegrationTestContext<TestableStartup<ReadWriteDbContext>, ReadWriteDbContext> _testContext;
        private readonly ReadWriteFakers _fakers = new ReadWriteFakers();

        public AddToToManyRelationshipTests(ExampleIntegrationTestContext<TestableStartup<ReadWriteDbContext>, ReadWriteDbContext> testContext)
        {
            _testContext = testContext;
        }

        [Fact]
        public async Task Cannot_add_to_HasOne_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            var existingUserAccount = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.AddRange(existingWorkItem, existingUserAccount);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new
                {
                    type = "userAccounts",
                    id = existingUserAccount.StringId
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/assignee";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.Forbidden);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            error.Title.Should().Be("Only to-many relationships can be updated through this endpoint.");
            error.Detail.Should().Be("Relationship 'assignee' must be a to-many relationship.");
        }

        [Fact]
        public async Task Can_add_to_HasMany_relationship_with_already_assigned_resources()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            existingWorkItem.Subscribers = _fakers.UserAccount.Generate(2).ToHashSet();

            var existingSubscriber = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.AddRange(existingWorkItem, existingSubscriber);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = existingWorkItem.Subscribers.ElementAt(1).StringId
                    },
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                var workItemInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.Subscribers)
                    .FirstWithIdAsync(existingWorkItem.Id);

                workItemInDatabase.Subscribers.Should().HaveCount(3);
                workItemInDatabase.Subscribers.Should().ContainSingle(subscriber => subscriber.Id == existingWorkItem.Subscribers.ElementAt(0).Id);
                workItemInDatabase.Subscribers.Should().ContainSingle(subscriber => subscriber.Id == existingWorkItem.Subscribers.ElementAt(1).Id);
                workItemInDatabase.Subscribers.Should().ContainSingle(subscriber => subscriber.Id == existingSubscriber.Id);
            });
        }

        [Fact]
        public async Task Can_add_to_HasManyThrough_relationship_with_already_assigned_resources()
        {
            // Arrange
            var existingWorkItems = _fakers.WorkItem.Generate(2);
            existingWorkItems[0].WorkItemTags = new[]
            {
                new WorkItemTag
                {
                    Tag = _fakers.WorkTag.Generate()
                }
            };
            existingWorkItems[1].WorkItemTags = new[]
            {
                new WorkItemTag
                {
                    Tag = _fakers.WorkTag.Generate()
                }
            };

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.AddRange(existingWorkItems);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "workTags",
                        id = existingWorkItems[0].WorkItemTags.ElementAt(0).Tag.StringId
                    },
                    new
                    {
                        type = "workTags",
                        id = existingWorkItems[1].WorkItemTags.ElementAt(0).Tag.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItems[0].StringId}/relationships/tags";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                // @formatter:wrap_chained_method_calls chop_always
                // @formatter:keep_existing_linebreaks true

                var workItemsInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.WorkItemTags)
                    .ThenInclude(workItemTag => workItemTag.Tag)
                    .ToListAsync();

                // @formatter:keep_existing_linebreaks restore
                // @formatter:wrap_chained_method_calls restore

                var workItemInDatabase1 = workItemsInDatabase.Single(workItem => workItem.Id == existingWorkItems[0].Id);

                int tagId1 = existingWorkItems[0].WorkItemTags.ElementAt(0).Tag.Id;
                int tagId2 = existingWorkItems[1].WorkItemTags.ElementAt(0).Tag.Id;

                workItemInDatabase1.WorkItemTags.Should().HaveCount(2);
                workItemInDatabase1.WorkItemTags.Should().ContainSingle(workItemTag => workItemTag.Tag.Id == tagId1);
                workItemInDatabase1.WorkItemTags.Should().ContainSingle(workItemTag => workItemTag.Tag.Id == tagId2);

                var workItemInDatabase2 = workItemsInDatabase.Single(workItem => workItem.Id == existingWorkItems[1].Id);

                workItemInDatabase2.WorkItemTags.Should().HaveCount(1);
                workItemInDatabase2.WorkItemTags.ElementAt(0).Tag.Id.Should().Be(tagId2);
            });
        }

        [Fact]
        public async Task Cannot_add_for_missing_request_body()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = string.Empty;

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.BadRequest);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.Title.Should().Be("Missing request body.");
            error.Detail.Should().BeNull();
        }

        [Fact]
        public async Task Cannot_add_for_missing_type()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        id = 99999999
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            error.Title.Should().Be("Failed to deserialize request body: Request body must include 'type' element.");
            error.Detail.Should().StartWith("Expected 'type' element in 'data' element. - Request body: <<");
        }

        [Fact]
        public async Task Cannot_add_for_unknown_type()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "doesNotExist",
                        id = 99999999
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            error.Title.Should().Be("Failed to deserialize request body: Request body includes unknown resource type.");
            error.Detail.Should().StartWith("Resource type 'doesNotExist' does not exist. - Request body: <<");
        }

        [Fact]
        public async Task Cannot_add_for_missing_ID()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts"
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            error.Title.Should().Be("Failed to deserialize request body: Request body must include 'id' element.");
            error.Detail.Should().StartWith("Request body: <<");
        }

        [Fact]
        public async Task Cannot_add_unknown_IDs_to_HasMany_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = 88888888
                    },
                    new
                    {
                        type = "userAccounts",
                        id = 99999999
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);

            responseDocument.Errors.Should().HaveCount(2);

            var error1 = responseDocument.Errors[0];
            error1.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error1.Title.Should().Be("A related resource does not exist.");
            error1.Detail.Should().Be("Related resource of type 'userAccounts' with ID '88888888' in relationship 'subscribers' does not exist.");

            var error2 = responseDocument.Errors[1];
            error2.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error2.Title.Should().Be("A related resource does not exist.");
            error2.Detail.Should().Be("Related resource of type 'userAccounts' with ID '99999999' in relationship 'subscribers' does not exist.");
        }

        [Fact]
        public async Task Cannot_add_unknown_IDs_to_HasManyThrough_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "workTags",
                        id = 88888888
                    },
                    new
                    {
                        type = "workTags",
                        id = 99999999
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/tags";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);

            responseDocument.Errors.Should().HaveCount(2);

            var error1 = responseDocument.Errors[0];
            error1.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error1.Title.Should().Be("A related resource does not exist.");
            error1.Detail.Should().Be("Related resource of type 'workTags' with ID '88888888' in relationship 'tags' does not exist.");

            var error2 = responseDocument.Errors[1];
            error2.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error2.Title.Should().Be("A related resource does not exist.");
            error2.Detail.Should().Be("Related resource of type 'workTags' with ID '99999999' in relationship 'tags' does not exist.");
        }

        [Fact]
        public async Task Cannot_add_to_unknown_resource_type_in_url()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            var existingSubscriber = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.AddRange(existingWorkItem, existingSubscriber);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    }
                }
            };

            var route = $"/doesNotExist/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);

            responseDocument.Should().BeEmpty();
        }

        [Fact]
        public async Task Cannot_add_to_unknown_resource_ID_in_url()
        {
            // Arrange
            var existingSubscriber = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.UserAccounts.Add(existingSubscriber);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    }
                }
            };

            const string route = "/workItems/99999999/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.Title.Should().Be("The requested resource does not exist.");
            error.Detail.Should().Be("Resource of type 'workItems' with ID '99999999' does not exist.");
        }

        [Fact]
        public async Task Cannot_add_to_unknown_relationship_in_url()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = 99999999
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/doesNotExist";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NotFound);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.NotFound);
            error.Title.Should().Be("The requested relationship does not exist.");
            error.Detail.Should().Be("Resource of type 'workItems' does not contain a relationship named 'doesNotExist'.");
        }

        [Fact]
        public async Task Cannot_add_for_relationship_mismatch_between_url_and_body()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            var existingSubscriber = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.AddRange(existingWorkItem, existingSubscriber);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/tags";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.Conflict);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.Conflict);
            error.Title.Should().Be("Resource type mismatch between request body and endpoint URL.");
            error.Detail.Should().Be("Expected resource of type 'workTags' in POST request body at endpoint " +
                $"'/workItems/{existingWorkItem.StringId}/relationships/tags', instead of 'userAccounts'.");
        }

        [Fact]
        public async Task Can_add_with_duplicates()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            var existingSubscriber = _fakers.UserAccount.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.AddRange(existingWorkItem, existingSubscriber);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    },
                    new
                    {
                        type = "userAccounts",
                        id = existingSubscriber.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                var workItemInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.Subscribers)
                    .FirstWithIdAsync(existingWorkItem.Id);

                workItemInDatabase.Subscribers.Should().HaveCount(1);
                workItemInDatabase.Subscribers.Single().Id.Should().Be(existingSubscriber.Id);
            });
        }

        [Fact]
        public async Task Can_add_with_empty_list()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new object[0]
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                var workItemInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.Subscribers)
                    .FirstWithIdAsync(existingWorkItem.Id);

                workItemInDatabase.Subscribers.Should().HaveCount(0);
            });
        }

        [Fact]
        public async Task Cannot_add_with_null_data_in_HasMany_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = (object)null
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/subscribers";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            error.Title.Should().Be("Failed to deserialize request body: Expected data[] element for to-many relationship.");
            error.Detail.Should().StartWith("Expected data[] element for 'subscribers' relationship. - Request body: <<");
        }

        [Fact]
        public async Task Cannot_add_with_null_data_in_HasManyThrough_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = (object)null
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/tags";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<ErrorDocument>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.UnprocessableEntity);

            responseDocument.Errors.Should().HaveCount(1);

            var error = responseDocument.Errors[0];
            error.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            error.Title.Should().Be("Failed to deserialize request body: Expected data[] element for to-many relationship.");
            error.Detail.Should().StartWith("Expected data[] element for 'tags' relationship. - Request body: <<");
        }

        [Fact]
        public async Task Can_add_self_to_cyclic_HasMany_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            existingWorkItem.Children = _fakers.WorkItem.Generate(1);

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "workItems",
                        id = existingWorkItem.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/children";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                var workItemInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.Children)
                    .FirstWithIdAsync(existingWorkItem.Id);

                workItemInDatabase.Children.Should().HaveCount(2);
                workItemInDatabase.Children.Should().ContainSingle(workItem => workItem.Id == existingWorkItem.Children[0].Id);
                workItemInDatabase.Children.Should().ContainSingle(workItem => workItem.Id == existingWorkItem.Id);
            });
        }

        [Fact]
        public async Task Can_add_self_to_cyclic_HasManyThrough_relationship()
        {
            // Arrange
            var existingWorkItem = _fakers.WorkItem.Generate();
            existingWorkItem.RelatedToItems = new List<WorkItemToWorkItem>
            {
                new WorkItemToWorkItem
                {
                    ToItem = _fakers.WorkItem.Generate()
                }
            };

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                dbContext.WorkItems.Add(existingWorkItem);
                await dbContext.SaveChangesAsync();
            });

            var requestBody = new
            {
                data = new[]
                {
                    new
                    {
                        type = "workItems",
                        id = existingWorkItem.StringId
                    }
                }
            };

            var route = $"/workItems/{existingWorkItem.StringId}/relationships/relatedTo";

            // Act
            var (httpResponse, responseDocument) = await _testContext.ExecutePostAsync<string>(route, requestBody);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.NoContent);

            responseDocument.Should().BeEmpty();

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                // @formatter:wrap_chained_method_calls chop_always
                // @formatter:keep_existing_linebreaks true

                var workItemInDatabase = await dbContext.WorkItems
                    .Include(workItem => workItem.RelatedToItems)
                    .ThenInclude(workItemToWorkItem => workItemToWorkItem.ToItem)
                    .FirstWithIdAsync(existingWorkItem.Id);

                // @formatter:keep_existing_linebreaks restore
                // @formatter:wrap_chained_method_calls restore

                int toItemId = existingWorkItem.RelatedToItems[0].ToItem.Id;

                workItemInDatabase.RelatedToItems.Should().HaveCount(2);
                workItemInDatabase.RelatedToItems.Should().OnlyContain(workItemToWorkItem => workItemToWorkItem.FromItem.Id == existingWorkItem.Id);
                workItemInDatabase.RelatedToItems.Should().ContainSingle(workItemToWorkItem => workItemToWorkItem.ToItem.Id == existingWorkItem.Id);
                workItemInDatabase.RelatedToItems.Should().ContainSingle(workItemToWorkItem => workItemToWorkItem.ToItem.Id == toItemId);
            });
        }
    }
}
