using System.Collections.Generic;
using System.Linq;
using JsonApiDotNetCore.Hooks.Internal.Execution;
using JsonApiDotNetCoreExample.Models;
using Moq;
using Xunit;

namespace UnitTests.ResourceHooks.Executor.Read
{
    public sealed class IdentifiableManyToManyAfterReadTests : HooksTestsSetup
    {
        private readonly ResourceHook[] _targetHooks = { ResourceHook.AfterRead };

        [Fact]
        public void AfterRead()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(_targetHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(_targetHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(_targetHooks, DisableDbValues);
            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);
            var (articles, joins, tags) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            articleResourceMock.Verify(rd => rd.AfterRead(It.IsAny<HashSet<Article>>(), ResourcePipeline.Get, false), Times.Once());
            joinResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<IdentifiableArticleTag>>(collection => !collection.Except(joins).Any()), ResourcePipeline.Get, true), Times.Once());
            tagResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<Tag>>(collection => !collection.Except(tags).Any()), ResourcePipeline.Get, true), Times.Once());
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }

        [Fact]
        public void AfterRead_Without_Parent_Hook_Implemented()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(NoHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(_targetHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(_targetHooks, DisableDbValues);
            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);
            var (articles, joins, tags) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            joinResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<IdentifiableArticleTag>>(collection => !collection.Except(joins).Any()), ResourcePipeline.Get, true), Times.Once());
            tagResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<Tag>>(collection => !collection.Except(tags).Any()), ResourcePipeline.Get, true), Times.Once());
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }

        [Fact]
        public void AfterRead_Without_Children_Hooks_Implemented()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(_targetHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(NoHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(_targetHooks, DisableDbValues);

            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);

            var (articles, _, tags) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            articleResourceMock.Verify(rd => rd.AfterRead(It.IsAny<HashSet<Article>>(), ResourcePipeline.Get, false), Times.Once());
            tagResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<Tag>>(collection => !collection.Except(tags).Any()), ResourcePipeline.Get, true), Times.Once());
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }

        [Fact]
        public void AfterRead_Without_Grand_Children_Hooks_Implemented()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(_targetHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(_targetHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(NoHooks, DisableDbValues);
            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);
            var (articles, joins, _) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            articleResourceMock.Verify(rd => rd.AfterRead(It.IsAny<HashSet<Article>>(), ResourcePipeline.Get, false), Times.Once());
            joinResourceMock.Verify(rd => rd.AfterRead(It.Is<HashSet<IdentifiableArticleTag>>(collection => !collection.Except(joins).Any()), ResourcePipeline.Get, true), Times.Once());
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }

        [Fact]
        public void AfterRead_Without_Any_Descendant_Hooks_Implemented()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(_targetHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(NoHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(NoHooks, DisableDbValues);
            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);
            var (articles, _, _) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            articleResourceMock.Verify(rd => rd.AfterRead(It.IsAny<HashSet<Article>>(), ResourcePipeline.Get, false), Times.Once());
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }

        [Fact]
        public void AfterRead_Without_Any_Hook_Implemented()
        {
            // Arrange
            var articleDiscovery = SetDiscoverableHooks<Article>(NoHooks, DisableDbValues);
            var joinDiscovery = SetDiscoverableHooks<IdentifiableArticleTag>(NoHooks, DisableDbValues);
            var tagDiscovery = SetDiscoverableHooks<Tag>(NoHooks, DisableDbValues);
            var (_, hookExecutor, articleResourceMock, joinResourceMock, tagResourceMock) = CreateTestObjects(articleDiscovery, joinDiscovery, tagDiscovery);
            var (articles, _, _) = CreateIdentifiableManyToManyData();

            // Act
            hookExecutor.AfterRead(articles, ResourcePipeline.Get);

            // Assert
            VerifyNoOtherCalls(articleResourceMock, joinResourceMock, tagResourceMock);
        }
    }
}

