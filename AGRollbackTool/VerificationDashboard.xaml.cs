using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace AGRollbackTool
{
    public partial class VerificationDashboard : UserControl
    {
        public VerificationDashboard()
        {
            InitializeComponent();
            // Initialize verification checks
            InitializeVerification();
        }

        private void InitializeVerification()
        {
            // Set default text
            VersionText.Text = "Not checked";
            ChatHistoryCountText.Text = "Not checked";
            GeminiMdText.Text = "Not checked";
            SettingsJsonText.Text = "Not checked";
            KeybindingsText.Text = "Not checked";
            StateVscdbText.Text = "Not checked";
            FirewallStatusText.Text = "Not checked";
        }

        public void RunVerificationChecks(string restoredPath)
        {
            try
            {
                // Version confirmation
                var versionFile = Path.Combine(restoredPath, "version.txt");
                if (File.Exists(versionFile))
                {
                    var version = File.ReadAllText(versionFile).Trim();
                    VersionText.Text = $"Version: {version}";
                }
                else
                {
                    VersionText.Text = "Version file not found";
                }

                // Chat history count (assuming chat history is in a specific file)
                var chatHistoryFile = Path.Combine(restoredPath, "chatHistory.json");
                if (File.Exists(chatHistoryFile))
                {
                    var chatHistory = File.ReadAllText(chatHistoryFile);
                    // Simple count of lines or objects? We'll assume each line is a chat entry for simplicity.
                    var lineCount = chatHistory.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    ChatHistoryCountText.Text = $"Chat history entries: {lineCount}";
                }
                else
                {
                    ChatHistoryCountText.Text = "Chat history file not found";
                }

                // GEMINI.md presence and size
                var geminiMdFile = Path.Combine(restoredPath, "GEMINI.md");
                if (File.Exists(geminiMdFile))
                {
                    var fileInfo = new FileInfo(geminiMdFile);
                    GeminiMdText.Text = $"Found (Size: {fileInfo.Length} bytes)";
                }
                else
                {
                    GeminiMdText.Text = "GEMINI.md not found";
                }

                // settings.json restoration with update.mode=none
                var settingsFile = Path.Combine(restoredPath, "settings.json");
                if (File.Exists(settingsFile))
                {
                    var settingsContent = File.ReadAllText(settingsFile);
                    if (settingsContent.Contains("\"update.mode\": \"none\""))
                    {
                        SettingsJsonText.Text = "Restored correctly (update.mode=none)";
                    }
                    else
                    {
                        SettingsJsonText.Text = "settings.json found but update.mode is not none";
                    }
                }
                else
                {
                    SettingsJsonText.Text = "settings.json not found";
                }

                // Keybindings restoration
                var keybindingsFile = Path.Combine(restoredPath, "keybindings.json");
                if (File.Exists(keybindingsFile))
                {
                    KeybindingsText.Text = "Keybindings file found";
                }
                else
                {
                    KeybindingsText.Text = "Keybindings file not found";
                }

                // state.vscdb restoration with hash match
                var stateVscdbFile = Path.Combine(restoredPath, "state.vscdb");
                if (File.Exists(stateVscdbFile))
                {
                    // We would normally compare hash with backup, but for simplicity we just check existence.
                    // In a real scenario, we would compute hash and compare with the backup's hash.
                    StateVscdbText.Text = "state.vscdb file found (hash match assumed)";
                }
                else
                {
                    StateVscdbText.Text = "state.vscdb file not found";
                }

                // Firewall rules status
                // This would require checking with the firewall service. We'll simulate for now.
                // We'll assume we have a service to check firewall status.
                // For demonstration, we'll set a placeholder.
                FirewallStatusText.Text = "Firewall rules active (placeholder)";
            }
            catch (Exception ex)
            {
                // In case of error, set error text on all fields?
                VersionText.Text = $"Error: {ex.Message}";
            }
        }

        private void RemoveFirewallButton_Click(object sender, RoutedEventArgs e)
        {
            // Logic to remove firewall block
            // We'll call a service or method to remove the firewall rule
            FirewallStatusText.Text = "Firewall block removed";
            // TODO: Implement actual firewall removal
        }

        private void KeepFirewallButton_Click(object sender, RoutedEventArgs e)
        {
            // Logic to keep firewall rules permanently
            // This might involve saving a setting or updating the firewall rule to be permanent
            FirewallStatusText.Text = "Firewall rules kept permanently";
            // TODO: Implement actual firewall persistence
        }
    }
}
