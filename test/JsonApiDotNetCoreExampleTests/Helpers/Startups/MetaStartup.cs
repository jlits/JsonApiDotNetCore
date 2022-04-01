using System;

using JsonApiDotNetCore.Extensions;
using JsonApiDotNetCore.Services;

using JsonApiDotNetCoreExample;
using JsonApiDotNetCoreExample.Data;

using JsonApiDotNetCoreExampleTests.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JsonApiDotNetCoreExampleTests.Startups;

public class MetaStartup : Startup
{
    #region Constructors

    public MetaStartup(IHostingEnvironment env)
        : base(env)
    {
    }

    #endregion

    #region Methods

    public override IServiceProvider ConfigureServices(IServiceCollection services)
    {
        var loggerFactory = new LoggerFactory();

        //loggerFactory.AddConsole(LogLevel.Warning);

        services
            .AddSingleton<ILoggerFactory>(loggerFactory)
            .AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(GetDbConnectionString()), ServiceLifetime.Transient)
            .AddJsonApi<AppDbContext>(options =>
            {
                options.Namespace = "api/v1";
                options.DefaultPageSize = 5;
                options.IncludeTotalRecordCount = true;
            })
            .AddScoped<IRequestMeta, MetaService>();

        return services.BuildServiceProvider();
    }

    #endregion
}
