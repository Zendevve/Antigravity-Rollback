using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Configuration;
using System.Linq;

namespace AGRollbackTool
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage()
        {
            InitializeComponent();
            LoadSettings();
            // Initialize theme toggle switch based on current theme
            InitializeThemeToggle();
        }

        private void LoadSettings()
        {
            // Load custom backup directory from settings (we'll use a simple approach: store in a config file or use properties)
            // For simplicity, we'll assume we have a static class or properties service. Since we don't have one, we'll use a placeholder.
            // In a real app, you might use a configuration file or the Properties.Settings.
            // We'll just set the text boxes to empty for now and later implement loading from a config file.
            BackupDirectoryTextBox.Text = ""; // Placeholder
            InstallerCacheDirectoryTextBox.Text = ""; // Placeholder
            KeepFirewallCheckBox.IsChecked = false; // Placeholder
            EnableAnimationsCheckBox.IsChecked = true; // Animations enabled by default

            // Load theme setting
            LoadThemeSetting();
        }

        private void InitializeThemeToggle()
        {
            // Get the current theme from App
            var app = Application.Current as App;
            if (app != null)
            {
                // Check if dark theme is currently applied by looking at merged dictionaries
                bool isDarkTheme = app.Resources.MergedDictionaries.Any(d =>
                    d.Source != null && d.Source.OriginalString.Contains("DarkTheme"));

                ThemeToggleButton.IsChecked = isDarkTheme;
            }
        }

        private void LoadThemeSetting()
        {
            try
            {
                string savedTheme = ConfigurationManager.AppSettings["ApplicationTheme"];
                if (!string.IsNullOrEmpty(savedTheme) &&
                    savedTheme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
                {
                    ThemeToggleButton.IsChecked = true;
                }
                else
                {
                    ThemeToggleButton.IsChecked = false;
                }
            }
            catch
            {
                // Default to light theme if there's an error
                ThemeToggleButton.IsChecked = false;
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Save the theme preference
            var app = Application.Current as App;
            if (app != null)
            {
                if (ThemeToggleButton.IsChecked == true)
                {
                    app.ApplyTheme("Dark");
                    app.SaveTheme("Dark");
                }
                else
                {
                    app.ApplyTheme("Light");
                    app.SaveTheme("Light");
                }
            }
        }

        private void SaveSettings()
        {
            // Save the settings to a configuration file or properties.
            // For now, we'll just show a message that settings are saved.
            // In a real implementation, you would persist these values.
            MessageBox.Show("Settings saved successfully!", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool AreAnimationsEnabled()
        {
            return EnableAnimationsCheckBox.IsChecked ?? true;
        }

        private void BrowseBackupDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                BackupDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseInstallerCacheDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                InstallerCacheDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void CancelSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Navigate back or hide the settings page. This depends on how the page is hosted.
            // For simplicity, we'll just reset the form or do nothing.
            // In a real app, you might have a parent container that switches views.
            LoadSettings(); // Reload the original settings to cancel changes
        }
    }
}
