using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AGRollbackTool;

namespace AGRollbackTool.Services
{
    /// <summary>
    /// Dependency injection service container for the AG Rollback Tool.
    /// Provides service registration and resolution.
    /// </summary>
    public class ServiceContainer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceContainer"/> class.
        /// </summary>
        public ServiceContainer() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceContainer"/> class with a pre-built configuration.
        /// </summary>
        /// <param name="configuration">The Microsoft.Extensions.Configuration instance.</param>
        public ServiceContainer(IConfiguration? configuration)
        {
            _services = new ServiceCollection();
            ConfigureServices(_services, configuration);
            _serviceProvider = _services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures all services for dependency injection.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="configuration">Optional configuration instance.</param>
        private void ConfigureServices(IServiceCollection services, IConfiguration? configuration)
        {
            // Register configuration first
            if (configuration != null)
            {
                services.AddSingleton<IConfigurationService>(new ConfigurationService(configuration));
            }
            else
            {
                // Build configuration if not provided
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                var config = configBuilder.Build();
                services.AddSingleton<IConfigurationService>(new ConfigurationService(config));
            }

            // Register PathResolver as singleton
            services.AddSingleton<IPathResolver, PathResolver>();

            // Register core services
            services.AddSingleton<IBackupService, BackupService>();
            services.AddSingleton<IRestoreService, RestoreService>();
            services.AddSingleton<IPurgeService, PurgeService>();
            services.AddSingleton<IProcessKiller, ProcessKiller>();
            services.AddSingleton<INetworkBlackoutService, NetworkBlackoutService>();

            // Register installer-related services
            services.AddSingleton<IInstallRunnerService, InstallRunnerService>();
            services.AddSingleton<IInstallerVersionService, InstallerVersionService>();
            services.AddSingleton<IVersionDetectorService, VersionDetectorService>();
            services.AddSingleton<IAntigravityInstallationService, AntigravityInstallationService>();
            services.AddSingleton<ISettingsInjectorService, SettingsInjectorService>();

            // Register the orchestrator service (depends on many other services)
            services.AddSingleton<IRollbackOrchestratorService, RollbackOrchestratorService>();
        }

        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance.</returns>
        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service instance, or null if not registered.</returns>
        public T? TryGetService<T>() where T : class
        {
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// Gets the underlying service provider.
        /// </summary>
        public IServiceProvider ServiceProvider => _serviceProvider;
    }
}
