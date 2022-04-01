using System;

using JsonApiDotNetCore.Extensions;

using JsonApiDotNetCoreExample.Data;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OperationsExample;

public class Startup
{
    #region Constructors

    public Startup(IHostingEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables();

        Config = builder.Build();
    }

    #endregion

    #region Fields

    public readonly IConfiguration Config;

    #endregion

    #region Methods

    public virtual IServiceProvider ConfigureServices(IServiceCollection services)
    {
        var loggerFactory = new LoggerFactory();

        //loggerFactory.AddConsole(LogLevel.Warning);

        services.AddSingleton<ILoggerFactory>(loggerFactory);

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(GetDbConnectionString()));

        services.AddJsonApi<AppDbContext>(opt => opt.EnableOperations = true);

        return services.BuildServiceProvider();
    }

    public virtual void Configure(
        IApplicationBuilder app,
        IHostingEnvironment env,
        ILoggerFactory loggerFactory,
        AppDbContext context)
    {
        context.Database.EnsureCreated();

        //loggerFactory.AddConsole(Config.GetSection("Logging"));
        app.UseJsonApi();
    }

    public string GetDbConnectionString()
    {
        return Config["Data:DefaultConnection"];
    }

    #endregion
}
