using System.Configuration;
using System.Data;
using System.Windows;
using System.Security.Principal;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Serilog;
using Microsoft.Extensions.Configuration;
using AGRollbackTool.Services;

namespace AGRollbackTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Theme constants
    private const string LightThemeName = "Light";
    private const string DarkThemeName = "Dark";
    private const string ThemeSettingsKey = "ApplicationTheme";

    private IConfiguration? _configuration;
    private IConfigurationService? _configurationService;

    /// <summary>
    /// Gets the configuration service for accessing settings.
    /// </summary>
    public IConfigurationService ConfigurationService
    {
        get
        {
            if (_configurationService == null)
            {
                throw new InvalidOperationException("Configuration service not initialized. Call InitializeConfiguration first.");
            }
            return _configurationService;
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize configuration first (before Serilog to use log settings)
        InitializeConfiguration();

        // Initialize Serilog with configuration settings
        InitializeSerilog();

        // Set up global exception handlers
        SetupExceptionHandlers();

        base.OnStartup(e);

        Log.Information("AG Rollback Tool starting up...");

        if (!IsRunAsAdministrator())
        {
            // Attempt to restart with elevated privileges
            if (RestartAsAdministrator())
            {
                // If successful, shut down the current instance
                Log.Information("Restarting with elevated privileges");
                Current.Shutdown();
            }
            else
            {
                // If user declined or failed, exit the application
                Log.Warning("Failed to obtain administrator privileges");
                Current.Shutdown();
            }
        }

        Log.Information("Application running with administrator privileges");

        // Load and apply saved theme
        LoadTheme();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("AG Rollback Tool shutting down...");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>
    /// Sets up global exception handlers for unhandled exceptions.
    /// </summary>
    private void SetupExceptionHandlers()
    {
        // AppDomain.UnhandledException - for non-UI unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // DispatcherUnhandledException - for UI thread exceptions
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // TaskScheduler.UnobservedTaskException - for async task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Handles unhandled exceptions from AppDomain (non-UI exceptions).
    /// </summary>
    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Log.Error(exception, "Unhandled AppDomain exception occurred. IsTerminating: {IsTerminating}", e.IsTerminating);

        // Show error dialog to user
        ShowErrorDialog(
            "Unexpected Error",
            "An unexpected error occurred. The application will now close.",
            exception);

        // For fatal exceptions, we can't prevent the app from terminating
        if (e.IsTerminating)
        {
            Log.Fatal(exception, "Application terminating due to unhandled exception");
            Log.CloseAndFlush();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles unhandled exceptions from the UI thread.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI thread exception occurred");

        // Show error dialog to user
        ShowErrorDialog(
            "Application Error",
            "An error occurred while processing your request. The application will try to continue.",
            e.Exception);

        // Mark exception as handled to prevent app crash
        e.Handled = true;
    }

    /// <summary>
    /// Handles unobserved task exceptions from async operations.
    /// </summary>
    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unobserved task exception occurred");

        // Show error dialog to user
        ShowErrorDialog(
            "Background Task Error",
            "A background task encountered an error.",
            e.Exception);

        // Mark exception as observed to prevent app crash
        e.SetObserved();
    }

    /// <summary>
    /// Shows a user-friendly error dialog with error details.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The error message to display.</param>
    /// <param name="exception">The exception containing details (optional).</param>
    public static void ShowErrorDialog(string title, string message, Exception? exception = null)
    {
        string fullMessage = message;

        if (exception != null)
        {
            // Include exception details in the message
            fullMessage = $"{message}\n\nError Details:\n{exception.Message}";

            // Add stack trace for debugging
            if (exception.StackTrace != null)
            {
                fullMessage += $"\n\nStack Trace:\n{exception.StackTrace}";
            }

            // Include inner exception if present
            if (exception.InnerException != null)
            {
                fullMessage += $"\n\nInner Exception:\n{exception.InnerException.Message}";
            }
        }

        // Show MessageBox on the UI thread
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                fullMessage,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        });
    }

    /// <summary>
    /// Initializes configuration from appsettings.json.
    /// </summary>
    private void InitializeConfiguration()
    {
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            _configuration = builder.Build();
            _configurationService = new ConfigurationService(_configuration);

            Log.Information("Configuration loaded. BackupDirectory: {BackupDirectory}, MaxBackups: {MaxBackups}",
                _configurationService.BackupDirectory,
                _configurationService.MaxBackups);
        }
        catch (Exception ex)
        {
            // Handle configuration initialization errors
            Log.Error(ex, "Failed to initialize configuration");
            ShowErrorDialog(
                "Configuration Error",
                "Failed to load application settings. The application may not function correctly.",
                ex);
        }
    }

    /// <summary>
    /// Initializes Serilog with file and console sinks.
    /// </summary>
    private void InitializeSerilog()
    {
        try
        {
            // Ensure logs directory exists - use configuration or default
            string logDir = _configurationService?.LogDirectory ?? "logs";
            string logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDir);
            Directory.CreateDirectory(logsPath);

            // Get log level from configuration
            var logLevel = Serilog.Events.LogEventLevel.Information;
            var logLevelString = _configurationService?.LogLevel ?? "Information";
            if (Enum.TryParse<Serilog.Events.LogEventLevel>(logLevelString, out var parsedLevel))
            {
                logLevel = parsedLevel;
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    Path.Combine(logsPath, "agrollback-.log"),
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Serilog initialized. Log path: {LogPath}, LogLevel: {LogLevel}", logsPath, logLevelString);
        }
        catch (Exception ex)
        {
            // If Serilog initialization fails, try to log to event log
            try
            {
                EventLog.WriteEntry("AG Rollback Tool", $"Serilog initialization failed: {ex.Message}", EventLogEntryType.Error);
            }
            catch { /* Ignore if we can't write to event log */ }

            throw;
        }
    }

    /// <summary>
    /// Checks if the application is running with administrator privileges.
    /// </summary>
    /// <returns>True if running as administrator, false otherwise.</returns>
    private bool IsRunAsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Attempts to restart the application with elevated privileges.
    /// </summary>
    /// <returns>True if the elevation was successful (or initiated), false if user declined.</returns>
    private bool RestartAsAdministrator()
    {
        var processInfo = new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = Process.GetCurrentProcess().MainModule.FileName,
            Verb = "runas"
        };

        try
        {
            Process.Start(processInfo);
            return true;
        }
        catch (Win32Exception)
        {
            // The user refused the elevation prompt.
            return false;
        }
    }

    /// <summary>
    /// Loads the saved theme preference and applies it.
    /// </summary>
    private void LoadTheme()
    {
        try
        {
            string savedTheme = System.Configuration.ConfigurationManager.AppSettings[ThemeSettingsKey];
            if (!string.IsNullOrEmpty(savedTheme))
            {
                ApplyTheme(savedTheme);
            }
            else
            {
                // Default to light theme if no preference saved
                ApplyTheme(LightThemeName);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error loading theme, defaulting to light theme");
            // If there's any error loading the theme, default to light
            ApplyTheme(LightThemeName);
        }
    }

    /// <summary>
    /// Saves the current theme preference to application settings.
    /// </summary>
    /// <param name="themeName">The name of the theme to save.</param>
    public void SaveTheme(string themeName)
    {
        try
        {
            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[ThemeSettingsKey] != null)
            {
                config.AppSettings.Settings[ThemeSettingsKey].Value = themeName;
            }
            else
            {
                config.AppSettings.Settings.Add(ThemeSettingsKey, themeName);
            }
            config.Save(ConfigurationSaveMode.Modified);
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to save theme preference");
        }
    }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="themeName">The name of the theme to apply ("Light" or "Dark").</param>
    public void ApplyTheme(string themeName)
    {
        // Clear existing theme dictionaries
        var themeDictionaries = Resources.MergedDictionaries.Where(d =>
            d.Source != null && (d.Source.OriginalString.Contains("LightTheme") ||
                                 d.Source.OriginalString.Contains("DarkTheme"))).ToList();

        foreach (var dict in themeDictionaries)
        {
            Resources.MergedDictionaries.Remove(dict);
        }

        // Apply the new theme
        if (themeName.Equals(DarkThemeName, StringComparison.OrdinalIgnoreCase))
        {
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("DarkTheme.xaml", UriKind.Relative) });
        }
        else
        {
            // For light theme, we rely on the default theme (no additional dictionary needed)
            // Or we could add a LightTheme.xaml if we create one later
        }
    }
}
