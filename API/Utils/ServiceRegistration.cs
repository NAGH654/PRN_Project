using Repositories.Interfaces;
using Repositories.Repo;
using Services.Dtos;
using Services.Interfaces;
using Services.Implement;
using Repositories.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Utils
{
    public static class ServiceRegistration
    {
        /// <summary>
        /// Register application services here.
        /// Usage: services.AddServices(IConfiguration config);
        /// </summary>
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            // TODO: register services, e.g.
            services.AddDatabase(config);
            
            // Add repositories
            services.AddRepositories();

            services.Configure<StorageOptions>(config.GetSection("Storage"));
            services.AddScoped<IAssignmentService, AssignmentService>();
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<ISubmissionService, SubmissionService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddHostedService<JobWorker>(); // background worker

            return services;
        }

        private static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // TODO: register repositories, e.g.
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<ISubmissionRepository, SubmissionRepository>();

            return services;
        }

        private static void AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(nameof(ServiceRegistration));

                options.UseSqlServer(
                    config.GetConnectionString("Default"),
                    sqlServerOptions =>
                    {
                        sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null
                        );

                        logger.LogInformation("Configured SQL Server with retry on failure.");
                    });
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
                //options.EnableSensitiveDataLogging();
                //options.LogTo(Console.WriteLine, LogLevel.Information);
            });
        }
    }
}