using System;

using JsonApiDotNetCore.Data;
using JsonApiDotNetCore.Extensions;
using JsonApiDotNetCore.Models;
using JsonApiDotNetCore.Services;

using JsonApiDotNetCoreExample.Data;
using JsonApiDotNetCoreExample.Models.Entities;
using JsonApiDotNetCoreExample.Models.Resources;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ResourceEntitySeparationExample.Models;

namespace ResourceEntitySeparationExample;

public class Startup
{
    #region Constructors

    public Startup(IHostingEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, false)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
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

        services.AddDbContext<AppDbContext>(options => options
                .UseNpgsql(GetDbConnectionString()),
            ServiceLifetime.Transient);

        services.AddScoped<IDbContextResolver, DbContextResolver<AppDbContext>>();

        var mvcBuilder = services.AddMvcCore();

        services.AddJsonApi(options =>
        {
            options.Namespace = "api/v1";
            options.DefaultPageSize = 10;
            options.IncludeTotalRecordCount = true;
            options.BuildResourceGraph(builder =>
            {
                builder.AddResource<CourseResource>("courses");
                builder.AddResource<DepartmentResource>("departments");
                builder.AddResource<StudentResource>("students");
            });
        }, mvcBuilder);

        services.AddAutoMapper(_ => { });
        services.AddScoped<IResourceMapper, AutoMapperAdapter>();

        services
            .AddScoped<IResourceService<CourseResource, int>,
                EntityResourceService<CourseResource, CourseEntity, int>>();

        services
            .AddScoped<IResourceService<DepartmentResource, int>,
                EntityResourceService<DepartmentResource, DepartmentEntity, int>>();

        services
            .AddScoped<IResourceService<StudentResource, int>,
                EntityResourceService<StudentResource, StudentEntity, int>>();

        var provider = services.BuildServiceProvider();
        var appContext = provider.GetRequiredService<AppDbContext>();
        if (appContext == null)
            throw new ArgumentException();

        return provider;
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
