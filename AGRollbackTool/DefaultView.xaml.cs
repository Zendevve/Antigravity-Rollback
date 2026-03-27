using System;
using System.Windows;
using System.Windows.Controls;

namespace AGRollbackTool
{
    public partial class DefaultView : UserControl
    {
        public DefaultView()
        {
            InitializeComponent();
        }

        // These methods will be called from MainWindow.xaml.cs
        public void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            // This will be handled by MainWindow
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.BackupButton_Click(sender, e);
        }

        public void RollbackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RollbackButton_Click(sender, e);
        }

        public void RestoreOnlyButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RestoreOnlyButton_Click(sender, e);
        }

        public void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.BrowseButton_Click(sender, e);
        }

        public void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ShowSettingsPage();
        }
    }
}
