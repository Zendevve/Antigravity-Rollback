using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;
using AGRollbackTool.Services;

namespace AGRollbackTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IRollbackOrchestratorService _rollbackOrchestratorService;
    private readonly IAntigravityInstallationService _installationService;
    private readonly IInstallerVersionService _installerVersionService;
    private DefaultView _defaultView;
    private VerificationDashboard _verificationDashboard;
    private SettingsPage _settingsPage;
    private InstallationState _currentInstallationState;

    public MainWindow()
    {
        InitializeComponent();
        LoadSampleBackupData();

        // Initialize views
        _defaultView = new DefaultView();
        _verificationDashboard = new VerificationDashboard();
        _settingsPage = new SettingsPage();

        // Set initial view
        MainContentControl.Content = _defaultView;

        // Initialize services
        var backupService = new BackupService();
        var processKiller = new ProcessKiller();
        var purgeService = new PurgeService();
        var pathResolver = new PathResolver();
        var networkBlackoutService = new NetworkBlackoutService(pathResolver);
        var installRunnerService = new InstallRunnerService();
        var restoreService = new RestoreService();
        var settingsInjectorService = new SettingsInjectorService();
        var versionDetectorService = new VersionDetectorService();
        var installationService = new AntigravityInstallationService(pathResolver);
        var installerVersionService = new InstallerVersionService();

        _rollbackOrchestratorService = new RollbackOrchestratorService(
            backupService,
            processKiller,
            purgeService,
            networkBlackoutService,
            installRunnerService,
            restoreService,
            settingsInjectorService,
            versionDetectorService);

        _installationService = installationService;
        _installerVersionService = installerVersionService;

        // Subscribe to events
        _rollbackOrchestratorService.ProgressChanged += OnRollbackProgressChanged;
        _rollbackOrchestratorService.Completed += OnRollbackCompleted;
        _rollbackOrchestratorService.ErrorOccurred += OnRollbackErrorOccurred;

        // Subscribe to ErrorStateView events
        ErrorStateView.RetryClicked += ErrorStateView_RetryClicked;
        ErrorStateView.CancelClicked += ErrorStateView_CancelClicked;

        // Detect installation state and update UI accordingly
        DetectInstallationState();
    }

    /// <summary>
    /// Detects the current installation state and updates the UI accordingly.
    /// </summary>
    private void DetectInstallationState()
    {
        _currentInstallationState = _installationService.GetInstallationState();
        Log($"Installation state detected: {_currentInstallationState.StateCategory} - {_currentInstallationState.StateMessage}");

        // Update UI based on installation state
        UpdateUIForInstallationState(_currentInstallationState);
    }

    /// <summary>
    /// Updates the UI based on the current installation state.
    /// </summary>
    private void UpdateUIForInstallationState(InstallationState state)
    {
        // Get references to the UI elements from DefaultView
        var backupButton = _defaultView.FindName("BackupButton") as Button;
        var rollbackButton = _defaultView.FindName("RollbackButton") as Button;
        var restoreOnlyButton = _defaultView.FindName("RestoreOnlyButton") as Button;

        switch (state.StateCategory)
        {
            case InstallationStateCategory.Normal:
                // Normal state - all buttons should be enabled
                Log("Normal state: Antigravity is installed, all operations available.");
                break;

            case InstallationStateCategory.NotInstalled:
                // Nothing to back up - disable backup and rollback, enable restore only if applicable
                Log("Not installed state: No Antigravity installation detected.");

                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed on this system.\n\n" +
                    "Nothing to back up.\n\n" +
                    "To use this tool, first install Antigravity, then you can backup, rollback, or restore.",
                    "Nothing to Back Up",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                break;

            case InstallationStateCategory.RestoreOnly:
                // AG not installed but backups exist - enable restore only mode
                Log("Restore only state: Antigravity not installed but backups exist.");

                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed, but previous backups were found on this system.\n\n" +
                    "You can use 'Restore Only' to restore from an existing backup to reinstall Antigravity.\n\n" +
                    "Backup and Rollback operations are disabled because there is no current installation to work with.",
                    "Restore Only Mode",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                break;

            case InstallationStateCategory.Unknown:
            default:
                Log($"Unknown installation state: {state.StateMessage}");
                break;
        }
    }

    private void ErrorStateView_RetryClicked(object sender, EventArgs e)
    {
        // Hide error state and retry the last operation
        HideErrorState();
        // TODO: Implement retry logic based on last operation
        // For now, we'll just log that retry was clicked
        Log("Retry button clicked - retry logic would be implemented here");
    }

    private void ErrorStateView_CancelClicked(object sender, EventArgs e)
    {
        // Hide error state and reset UI
        HideErrorState();
        ShowDefaultView();
        Log("Operation cancelled by user");
    }

    private async void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if Antigravity is installed - if not, we can't backup
            if (_currentInstallationState.StateCategory == InstallationStateCategory.NotInstalled)
            {
                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed.\n\n" +
                    "There is nothing to back up because Antigravity is not present on this system.\n\n" +
                    "To use the backup feature, first install Antigravity.",
                    "Nothing to Back Up",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                Log("Backup cancelled: Antigravity is not installed.");
                return;
            }

            // Check for restore-only state
            if (_currentInstallationState.StateCategory == InstallationStateCategory.RestoreOnly)
            {
                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed.\n\n" +
                    "You cannot create a new backup because there is no current installation to back up.\n\n" +
                    "Use 'Restore Only' to restore from an existing backup.",
                    "Cannot Create Backup",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                Log("Backup cancelled: Not in a state to create backup (restore only mode).");
                return;
            }

            // Disable button during operation
            BackupButton.IsEnabled = false;

            Log("Backup process started...");
            StatusTextBlock.Text = "Backup in progress...";

            // Create rollback options for backup only
            var options = new RollbackOptions
            {
                SkipBackup = false,
                SkipKillProcesses = true,
                SkipPurge = true,
                SkipFirewallBlackout = true,
                SkipInstall = true,
                SkipScaffoldCreation = true,
                SkipRestore = true,
                SkipSettingsInjection = true,
                SkipVerification = true
            };

            await _rollbackOrchestratorService.StartRollbackAsync(options);
        }
        catch (Exception ex)
        {
            Log($"Backup failed: {ex.Message}");
            StatusTextBlock.Text = "Backup failed!";
            ShowErrorState("Backup failed", ex.Message, ex.StackTrace);
        }
        finally
        {
            // Re-enable button
            BackupButton.IsEnabled = true;
        }
    }

    private async void RollbackButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Check if Antigravity is installed - if not, we can't rollback
            if (_currentInstallationState.StateCategory == InstallationStateCategory.NotInstalled)
            {
                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed.\n\n" +
                    "There is nothing to rollback because Antigravity is not present on this system.\n\n" +
                    "To use the rollback feature, first install Antigravity.",
                    "Nothing to Rollback",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                Log("Rollback cancelled: Antigravity is not installed.");
                return;
            }

            // Check for restore-only state
            if (_currentInstallationState.StateCategory == InstallationStateCategory.RestoreOnly)
            {
                System.Windows.MessageBox.Show(
                    "Antigravity is not currently installed.\n\n" +
                    "You cannot perform a rollback because there is no current installation to work with.\n\n" +
                    "Use 'Restore Only' to restore from an existing backup.",
                    "Cannot Perform Rollback",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                Log("Rollback cancelled: Not in a state to rollback (restore only mode).");
                return;
            }

            // Check if we have any completed backups (safety check for purge)
            var hasCompletedBackup = BackupListView.Items.Cast<object>()
                .Any(item =>
                {
                    var backup = item as dynamic;
                    return backup != null && backup.Status == "Completed";
                });

            if (!hasCompletedBackup)
            {
                var result = System.Windows.MessageBox.Show(
                    "No completed backups found. The purge operation will delete the existing Antigravity installation.\n" +
                    "Without a backup, you will not be able to restore your data if something goes wrong.\n" +
                    "Do you want to continue anyway?",
                    "Safety Check: No Completed Backups",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    Log("Rollback cancelled by user due to no completed backups (safety check).");
                    return;
                }
            }

            // Show confirmation dialog
            var result2 = System.Windows.MessageBox.Show(
                "Are you sure you want to perform a full rollback? This will:\n" +
                "1. Backup current Antigravity data\n" +
                "2. Kill all Antigravity processes\n" +
                "3. Purge the existing Antigravity installation\n" +
                "4. Apply network blackout\n" +
                "5. Install the specified version\n" +
                "6. Create scaffold files\n" +
                "7. Restore data from backup\n" +
                "8. Inject user settings\n" +
                "9. Verify the installation\n\n" +
                "This operation cannot be interrupted once started.",
                "Confirm Full Rollback",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result2 != System.Windows.MessageBoxResult.Yes)
            {
                Log("Rollback cancelled by user.");
                return;
            }

            // Disable button during operation
            RollbackButton.IsEnabled = false;

            Log("Rollback process started...");
            StatusTextBlock.Text = "Rollback in progress...";

            // Create rollback options for full rollback
            var options = new RollbackOptions
            {
                SkipBackup = false,
                SkipKillProcesses = false,
                SkipPurge = false,
                SkipFirewallBlackout = false,
                SkipInstall = false,
                SkipScaffoldCreation = false,
                SkipRestore = false,
                SkipSettingsInjection = false,
                SkipVerification = false
            };

            await _rollbackOrchestratorService.StartRollbackAsync(options);
        }
        catch (Exception ex)
        {
            Log($"Rollback failed: {ex.Message}");
            StatusTextBlock.Text = "Rollback failed!";
            ShowErrorState("Rollback failed", ex.Message, ex.StackTrace);
        }
        finally
        {
            // Re-enable button
            RollbackButton.IsEnabled = true;
        }
    }

    private async void RestoreOnlyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // Show confirmation dialog
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to restore data from backup? This will:\n" +
                "1. Skip backup phase\n" +
                "2. Skip killing processes\n" +
                "3. Skip purge phase\n" +
                "4. Skip network blackout\n" +
                "5. Skip installation\n" +
                "6. Skip scaffold creation\n" +
                "7. Restore data from backup (will overwrite existing data)\n" +
                "8. Inject user settings\n" +
                "9. Verify the installation\n\n" +
                "This operation will overwrite your current Antigravity data with data from the backup.",
                "Confirm Restore Only",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
            {
                Log("Restore only cancelled by user.");
                return;
            }

            // Disable button during operation
            RestoreOnlyButton.IsEnabled = false;

            Log("Restore only process started...");
            StatusTextBlock.Text = "Restore only in progress...";

            // Create rollback options for restore only
            var options = new RollbackOptions
            {
                SkipBackup = true,
                SkipKillProcesses = true,
                SkipPurge = true,
                SkipFirewallBlackout = true,
                SkipInstall = true,
                SkipScaffoldCreation = true,
                SkipRestore = false,
                SkipSettingsInjection = false,
                SkipVerification = false
            };

            await _rollbackOrchestratorService.StartRollbackAsync(options);
        }
        catch (Exception ex)
        {
            Log($"Restore only failed: {ex.Message}");
            StatusTextBlock.Text = "Restore only failed!";
            ShowErrorState("Restore only failed", ex.Message, ex.StackTrace);
        }
        finally
        {
            // Re-enable button
            RestoreOnlyButton.IsEnabled = true;
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            Title = "Select Installer Executable"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            string installerPath = openFileDialog.FileName;
            InstallerPathTextBox.Text = installerPath;
            Log($"Selected installer: {installerPath}");

            // Check installer version against expected version
            CheckInstallerVersion(installerPath);
        }
    }

    /// <summary>
    /// Checks the installer version and shows a warning if it doesn't match expectations.
    /// </summary>
    private void CheckInstallerVersion(string installerPath)
    {
        if (string.IsNullOrEmpty(installerPath) || !System.IO.File.Exists(installerPath))
        {
            return;
        }

        // Get the expected version (from current installation or from user settings)
        VersionInfo? expectedVersion = null;

        // Try to get version from current installation
        if (_currentInstallationState.IsInstalled && _currentInstallationState.InstalledVersion != null)
        {
            expectedVersion = _currentInstallationState.InstalledVersion;
        }

        // Compare installer version with expected version
        var comparisonResult = _installerVersionService.CompareVersion(installerPath, expectedVersion);

        if (comparisonResult == VersionComparisonResult.Different)
        {
            // Show warning dialog and let user decide whether to proceed
            bool userConfirmed = _installerVersionService.ShowVersionWarningAndGetConfirmation(
                installerPath,
                expectedVersion);

            if (!userConfirmed)
            {
                Log("User cancelled due to version mismatch.");
                // Clear the installer path since user cancelled
                InstallerPathTextBox.Text = string.Empty;
            }
            else
            {
                Log("User chose to proceed despite version mismatch.");
            }
        }
        else if (comparisonResult == VersionComparisonResult.Unknown)
        {
            Log("Could not determine installer version.");
        }
        else
        {
            Log("Installer version matches expected version.");
        }
    }

    private void Log(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogConsoleTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
        // Auto-scroll to the bottom
        LogConsoleTextBox.ScrollToEnd();
    }

     private void LoadSampleBackupData()
     {
         // TODO: Replace with actual backup data from storage
         var sampleBackups = new[]
         {
             new { Timestamp = "2026-03-27 10:30", Version = "1.2.3", Status = "Completed" },
             new { Timestamp = "2026-03-26 14:15", Version = "1.2.2", Status = "Completed" },
             new { Timestamp = "2026-03-25 09:45", Version = "1.2.1", Status = "Failed" }
         };

         BackupListView.ItemsSource = sampleBackups;
     }

     private void OnRollbackProgressChanged(object sender, RollbackProgressChangedEventArgs e)
     {
         // Update UI with progress information
         var progress = e.Progress;
         StatusTextBlock.Text = progress.CurrentPhaseDescription;
         Log($"Progress: {progress.PercentageComplete}% - {progress.CurrentPhaseDescription}");

         // Update status stepper visuals based on current phase
         UpdateStatusStepper(progress.CurrentPhase);
     }

     private void OnRollbackCompleted(object sender, RollbackCompletedEventArgs e)
     {
         if (e.Success)
         {
             Log("Rollback completed successfully!");
             StatusTextBlock.Text = "Rollback completed successfully!";

             // Show verification dashboard after successful restore
             // We need to get the restored path from the event or service
             // For now, we'll use a placeholder path - in a real implementation,
             // this would come from the rollback service or event args
             string restoredPath = GetRestoredPath(); // TODO: Implement proper path retrieval
             _verificationDashboard.RunVerificationChecks(restoredPath);
             ShowVerificationDashboard();
         }
         else
         {
             Log($"Rollback failed: {e.Result.ErrorMessage}");
             StatusTextBlock.Text = "Rollback failed!";
         }

         // Reset UI state
         ResetUIState();
     }

     private void OnRollbackErrorOccurred(object sender, RollbackErrorEventArgs e)
     {
         Log($"Error during {e.Phase}: {e.Exception.Message}");
         StatusTextBlock.Text = $"Error during {e.Phase}";

         // Implement rollback-the-rollback logic here
         HandleRollbackError(e.Phase, e.Exception);
     }

      private void UpdateStatusStepper(RollbackPhase currentPhase)
      {
          // Reset all steps to default (incomplete) state
          SetStepState(Step1Ellipse, false);
          SetStepState(Step2Ellipse, false);
          SetStepState(Step3Ellipse, false);
          SetStepState(Step4Ellipse, false);

          // Update steps based on current phase
          switch (currentPhase)
          {
              case RollbackPhase.Backup:
                  SetStepState(Step1Ellipse, true); // Preparation complete
                  SetStepState(Step2Ellipse, true); // Backup in progress
                  break;
              case RollbackPhase.KillProcesses:
              case RollbackPhase.Purge:
              case RollbackPhase.FirewallBlackout:
              case RollbackPhase.Install:
              case RollbackPhase.ScaffoldCreation:
                  SetStepState(Step1Ellipse, true); // Preparation complete
                  SetStepState(Step2Ellipse, true); // Backup complete
                  SetStepState(Step3Ellipse, true); // Rollback in progress
                  break;
              case RollbackPhase.Restore:
              case RollbackPhase.SettingsInjection:
              case RollbackPhase.Verification:
                  SetStepState(Step1Ellipse, true); // Preparation complete
                  SetStepState(Step2Ellipse, true); // Backup complete
                  SetStepState(Step3Ellipse, true); // Rollback complete
                  SetStepState(Step4Ellipse, true); // Verification in progress
                  break;
              case RollbackPhase.Completed:
                  SetStepState(Step1Ellipse, true); // Preparation complete
                  SetStepState(Step2Ellipse, true); // Backup complete
                  SetStepState(Step3Ellipse, true); // Rollback complete
                  SetStepState(Step4Ellipse, true); // Verification complete
                  break;
              case RollbackPhase.Failed:
              case RollbackPhase.Cancelled:
                  // Keep current state to show where it failed
                  break;
              default:
                  // NotStarted - all steps reset
                  break;
          }

          // Show progress bar during operations
          ProgressBar.Visibility = (currentPhase != RollbackPhase.NotStarted &&
                                  currentPhase != RollbackPhase.Completed &&
                                  currentPhase != RollbackPhase.Failed &&
                                  currentPhase != RollbackPhase.Cancelled)
                                  ? System.Windows.Visibility.Visible
                                  : System.Windows.Visibility.Collapsed;
      }

      private void SetStepState(System.Windows.Shapes.Ellipse ellipse, bool isComplete)
      {
          if (_defaultView != null)
          {
              if (isComplete)
              {
                  ellipse.BeginAnimation(System.Windows.Shapes.Ellipse.FillProperty,
                      (System.Windows.Media.Animation.ColorAnimation)_defaultView.Resources["StepActiveAnimation"]);
              }
              else
              {
                  ellipse.BeginAnimation(System.Windows.Shapes.Ellipse.FillProperty,
                      (System.Windows.Media.Animation.ColorAnimation)_defaultView.Resources["StepInactiveAnimation"]);
              }
          }
          else
          {
              // Fallback if _defaultView is not initialized
              if (isComplete)
              {
                  ellipse.Fill = System.Windows.Media.Brushes.Green;
              }
              else
              {
                  ellipse.Fill = System.Windows.Media.Brushes.LightGray;
              }
          }
      }

     private void ResetUIState()
     {
         // Reset button states and UI elements
         Log("Resetting UI state");

         // Hide error state and show main content with animation
         ApplyFadeOutAnimation(() => {
             ErrorStateView.Visibility = System.Windows.Visibility.Collapsed;
             MainContentControl.Visibility = System.Windows.Visibility.Visible;
             ApplyFadeInAnimation();
         });
     }

      private void HandleRollbackError(RollbackPhase failedPhase, Exception exception)
      {
          // Implement rollback-the-rollback logic
          // If we failed during install or later phases but purge succeeded, we should restore from backup
          if (failedPhase >= RollbackPhase.Install &&
              (failedPhase == RollbackPhase.Install ||
               failedPhase == RollbackPhase.ScaffoldCreation ||
               failedPhase == RollbackPhase.Restore ||
               failedPhase == RollbackPhase.SettingsInjection ||
               failedPhase == RollbackPhase.Verification))
          {
              Log("Attempting to restore from backup due to failure in later phase...");

              // Start a restore-only operation to rollback the rollback
              try
              {
                  // Create rollback options for restore only
                  var options = new RollbackOptions
                  {
                      SkipBackup = true,
                      SkipKillProcesses = true,
                      SkipPurge = true,
                      SkipFirewallBlackout = true,
                      SkipInstall = true,
                      SkipScaffoldCreation = true,
                      SkipRestore = false,
                      SkipSettingsInjection = false,
                      SkipVerification = false
                  };

                  // Fire and forget the restore operation
                  _ = _rollbackOrchestratorService.StartRollbackAsync(options);
                  }

                  private string GetRestoredPath()
                  {
                      // TODO: Implement proper logic to get the restored path from the rollback service
                      // For now, we'll return a placeholder path
                      // In a real implementation, this would come from the RollbackCompletedEventArgs or a service
                      return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                  }

                  private void ShowVerificationDashboard()
                  {
                      ApplyFadeOutAnimation(() => {
                          MainContentControl.Content = _verificationDashboard;
                          ApplyFadeInAnimation();
                      });
                  }

                  private void ShowSettingsPage()
                  {
                      ApplyFadeOutAnimation(() => {
                          MainContentControl.Content = _settingsPage;
                          ApplyFadeInAnimation();
                      });
                  }

                  private void ShowDefaultView()
                  {
                      ApplyFadeOutAnimation(() => {
                          MainContentControl.Content = _defaultView;
                          ApplyFadeInAnimation();
                      });
                  }

                  private void ApplyFadeOutAnimation(Action onCompleted)
                  {
                      // Check if animations are enabled
                      if (_settingsPage != null && !_settingsPage.AreAnimationsEnabled())
                      {
                          onCompleted();
                          return;
                      }

                      if (MainContentControl.Content != null)
                      {
                          var storyboard = (Storyboard)Resources["FadeOutAnimation"];
                          storyboard.Completed += (s, e) => onCompleted();
                          storyboard.Begin(MainContentControl);
                      }
                      else
                      {
                          onCompleted();
                      }
                  }

                  private void ApplyFadeInAnimation()
                  {
                      // Check if animations are enabled
                      if (_settingsPage != null && !_settingsPage.AreAnimationsEnabled())
                      {
                          return;
                      }

                      var storyboard = (Storyboard)Resources["FadeInAnimation"];
                      storyboard.Begin(MainContentControl);
                  }

                  private void ShowErrorState(string errorMessage, string exceptionMessage, string stackTrace = null)
                  {
                      // Set the error information
                      ErrorStateView.SetError(errorMessage, exceptionMessage, stackTrace);

                      // Show the error state view with animation
                      ApplyFadeOutAnimation(() => {
                          MainContentControl.Visibility = System.Windows.Visibility.Collapsed;
                          ErrorStateView.Visibility = System.Windows.Visibility.Visible;
                          ApplyFadeInAnimation();
                      });
                  }

                  private void HideErrorState()
                  {
                      // Hide the error state view with animation
                      ApplyFadeOutAnimation(() => {
                          ErrorStateView.Visibility = System.Windows.Visibility.Collapsed;
                          MainContentControl.Visibility = System.Windows.Visibility.Visible;
                          ApplyFadeInAnimation();
                      });
                  }
              }

                private void ShowVerificationDashboard()
                {
                    MainContentControl.Content = _verificationDashboard;
                }

                private void ShowSettingsPage()
                {
                    MainContentControl.Content = _settingsPage;
                }

                private void ShowDefaultView()
                {
                    ApplyFadeOutAnimation(() => {
                        MainContentControl.Content = _defaultView;
                        ApplyFadeInAnimation();
                    });
                }
            }
              catch (Exception restoreEx)
               {
                   Log($"Rollback-the-rollback failed: {restoreEx.Message}");
               }
           }
           else if (failedPhase == RollbackPhase.Purge)
           {
               Log("Purge failed. The system may be in an inconsistent state.");
           }
       }

       private string GetRestoredPath()
       {
           return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
       }

       private void ShowVerificationDashboard()
       {
           MainContentControl.Content = _verificationDashboard;
       }

       private void ShowSettingsPage()
       {
           MainContentControl.Content = _settingsPage;
       }

       private void ShowDefaultView()
       {
           ApplyFadeOutAnimation(() => {
               MainContentControl.Content = _defaultView;
               ApplyFadeInAnimation();
           });
       }

       private void ApplyFadeOutAnimation(Action onCompleted)
       {
           if (_settingsPage != null && !_settingsPage.AreAnimationsEnabled())
           {
               onCompleted();
               return;
           }

           if (MainContentControl.Content != null)
           {
               var storyboard = (Storyboard)Resources["FadeOutAnimation"];
               storyboard.Completed += (s, e) => onCompleted();
               storyboard.Begin(MainContentControl);
           }
           else
           {
               onCompleted();
           }
       }

       private void ApplyFadeInAnimation()
       {
           if (_settingsPage != null && !_settingsPage.AreAnimationsEnabled())
           {
               return;
           }

           var storyboard = (Storyboard)Resources["FadeInAnimation"];
           storyboard.Begin(MainContentControl);
       }

       private void ShowErrorState(string errorMessage, string exceptionMessage, string stackTrace = null)
       {
           ErrorStateView.SetError(errorMessage, exceptionMessage, stackTrace);
           ApplyFadeOutAnimation(() => {
               MainContentControl.Visibility = System.Windows.Visibility.Collapsed;
               ErrorStateView.Visibility = System.Windows.Visibility.Visible;
               ApplyFadeInAnimation();
           });
       }

       private void HideErrorState()
       {
           ApplyFadeOutAnimation(() => {
               ErrorStateView.Visibility = System.Windows.Visibility.Collapsed;
               MainContentControl.Visibility = System.Windows.Visibility.Visible;
               ApplyFadeInAnimation();
           });
       }
   }
}
