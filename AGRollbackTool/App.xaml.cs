using System.Configuration;
using System.Data;
using System.Windows;
using System.Security.Principal;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;

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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!IsRunAsAdministrator())
        {
            // Attempt to restart with elevated privileges
            if (RestartAsAdministrator())
            {
                // If successful, shut down the current instance
                Current.Shutdown();
            }
            else
            {
                // If user declined or failed, exit the application
                Current.Shutdown();
            }
        }

        // Load and apply saved theme
        LoadTheme();
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
            string savedTheme = ConfigurationManager.AppSettings[ThemeSettingsKey];
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
        catch
        {
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
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[ThemeSettingsKey] != null)
            {
                config.AppSettings.Settings[ThemeSettingsKey].Value = themeName;
            }
            else
            {
                config.AppSettings.Settings.Add(ThemeSettingsKey, themeName);
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        catch
        {
            // Ignore errors in saving theme preference
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

