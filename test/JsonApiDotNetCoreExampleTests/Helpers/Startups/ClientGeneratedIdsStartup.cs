using System;

using JsonApiDotNetCore.Extensions;

using JsonApiDotNetCoreExample;
using JsonApiDotNetCoreExample.Data;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCoreExampleTests.Startups;

public class ClientGeneratedIdsStartup : Startup
{
    #region Constructors

    public ClientGeneratedIdsStartup(IHostingEnvironment env)
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
            opt.AllowClientGeneratedIds = true;
        });

        return services.BuildServiceProvider();
    }

    #endregion
}
