using System;

using JsonApiDotNetCore.Data;
using JsonApiDotNetCore.Extensions;
using JsonApiDotNetCore.Services;

using JsonApiDotNetCoreExample;
using JsonApiDotNetCoreExample.Data;
using JsonApiDotNetCoreExample.Models;

using JsonApiDotNetCoreExampleTests.Repositories;
using JsonApiDotNetCoreExampleTests.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using UnitTests;

namespace JsonApiDotNetCoreExampleTests.Startups;

public class AuthorizedStartup : Startup
{
    #region Constructors

    public AuthorizedStartup(IHostingEnvironment env)
        : base(env)
    {
    }

    #endregion

    #region Methods

    public override IServiceProvider ConfigureServices(IServiceCollection services)
    {
        var loggerFactory = new LoggerFactory();

        //loggerFactory.AddConsole();

        services.AddSingleton<ILoggerFactory>(loggerFactory);

        services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(GetDbConnectionString()); },
            ServiceLifetime.Transient);

        services.AddJsonApi<AppDbContext>(opt =>
        {
            opt.Namespace = "api/v1";
            opt.DefaultPageSize = 5;
            opt.IncludeTotalRecordCount = true;
        });

        // custom authorization implementation
        var authServicMock = new Mock<IAuthorizationService>();
        authServicMock.SetupAllProperties();
        services.AddSingleton(authServicMock.Object);
        services.AddScoped<IEntityRepository<TodoItem>, AuthorizedTodoItemsRepository>();

        services.AddScoped<IScopedServiceProvider, TestScopedServiceProvider>();

        return services.BuildServiceProvider();
    }

    #endregion
}
