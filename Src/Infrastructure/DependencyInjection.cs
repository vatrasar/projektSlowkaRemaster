using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.RepositoryContracts;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data;
using ProjektSlowkaRemasterd.Src.Infrastructure.Data.Repositories;

namespace ProjektSlowkaRemasterd.Src.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Configures services in the Microsoft.Extensions.DependencyInjection container.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // 1. Build and Register Configuration
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // 2. Bind AppConfig options
        services.Configure<AppConfig>(configuration.GetSection("AppConfig"));

        // Bind a temporary instance to resolve the database path during startup
        var appConfig = new AppConfig();
        configuration.GetSection("AppConfig").Bind(appConfig);
        var dbPath = appConfig.ResolvedDatabasePath;

        var dbDir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dbDir) && !Directory.Exists(dbDir))
        {
            Directory.CreateDirectory(dbDir);
        }

        var mediaPath = appConfig.ResolvedMediaDirectoryPath;
        if (!string.IsNullOrEmpty(mediaPath) && !Directory.Exists(mediaPath))
        {
            Directory.CreateDirectory(mediaPath);
        }

        // 3. Register DbContext
        services.AddDbContext<NameOfAppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Transient); // Avoid context sharing issues across viewmodels

        // 4. Register Repositories
        services.AddTransient<ICategoryRepository, CategoryRepository>();
        services.AddTransient<ITopicRepository, TopicRepository>();
        services.AddTransient<ISectionRepository, SectionRepository>();
        services.AddTransient<IQuestionRepository, QuestionRepository>();
        services.AddTransient<IStatisticsRepository, StatisticsRepository>();
        services.AddTransient<IMediaRepository, MediaRepository>();

        return services;
    }
}
