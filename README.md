# AG Rollback Tool

A WPF desktop application for managing backup, restore, and rollback operations for Google's Antigravity AI agent on Windows.

## Overview

AG Rollback Tool provides a comprehensive solution for managing your Antigravity AI agent installation. It enables you to create timestamped backups of all your data, perform complete system rollbacks to previous states, and restore from existing backups. The tool includes network blackout capabilities using Windows Firewall to prevent unwanted updates during rollback operations.

## Features

### Core Features

- **Timestamped Backup Creation**: Create automatic backups of all Antigravity data including:
  - Chat history and conversation data
  - User settings and preferences
  - Keyboard keybindings
  - Global storage state
  - GEMINI.md agent rules

- **Full Rollback**: Complete rollback workflow that:
  - Creates a fresh backup before rollback
  - Terminates all running Antigravity processes
  - Purges the current installation
  - Applies network blackout via Windows Firewall
  - Installs the specified version
  - Restores your data from backup
  - Verifies the integrity of the restored system

- **Restore Only**: Restore your data from any existing backup without performing a full rollback

- **Network Blackout**: Block Antigravity network access using Windows Firewall rules to prevent automatic updates during rollback

- **Verification Dashboard**: Comprehensive verification of:
  - Installed version confirmation
  - Chat history integrity
  - Settings restoration (including `update.mode=none`)
  - Keybindings restoration
  - Global storage state verification
  - Firewall rules status

### Additional Features

- **Dark Theme Support**: Toggle between light and dark themes
- **Administrator Privileges**: Automatically requests elevated privileges when needed
- **Progress Tracking**: Real-time progress updates during all operations
- **Error Handling**: Comprehensive error state views with recovery options

## Architecture

### Project Structure

```
AGRollbackTool/
├── Services/           # Core business logic services
├── Models/             # Data models and DTOs
├── *.xaml              # UI views and components
└── *.cs                # Code-behind and utilities
```

### Key Components

#### Services Layer

| Service | Description |
|---------|-------------|
| [`RollbackOrchestratorService`](AGRollbackTool/Services/RollbackOrchestratorService.cs) | Coordinates the complete rollback process across all phases |
| [`BackupService`](AGRollbackTool/Services/BackupService.cs) | Creates timestamped backups of Antigravity data |
| [`RestoreService`](AGRollbackTool/Services/RestoreService.cs) | Restores data from existing backups with hash verification |
| [`PurgeService`](AGRollbackTool/Services/PurgeService.cs) | Removes Antigravity installation files, registry keys, and shortcuts |
| [`ProcessKiller`](AGRollbackTool/Services/ProcessKiller.cs) | Terminates running Antigravity processes |
| [`NetworkBlackoutService`](AGRollbackTool/Services/NetworkBlackoutService.cs) | Manages Windows Firewall rules to block Antigravity network access |
| [`AntigravityInstallationService`](AGRollbackTool/Services/AntigravityInstallationService.cs) | Detects installation status and version |
| [`SettingsInjectorService`](AGRollbackTool/Services/SettingsInjectorService.cs) | Injects settings into restored configuration |

#### Models

| Model | Description |
|-------|-------------|
| [`BackupEntry`](AGRollbackTool/Models/BackupEntry.cs) | Represents a single file or directory in a backup |
| [`BackupManifest`](AGRollbackTool/Models/BackupManifest.cs) | Metadata for a backup including timestamp, version, and file list |
| [`RollbackSession`](AGRollbackTool/Models/RollbackSession.cs) | Tracks the state of a rollback operation |

#### Path Resolution

The application resolves Antigravity data from these locations:

- `%USERPROFILE%/.gemini/antigravity/` - Chat history, settings, keybindings
- `%APPDATA%/antigravity/` - Application data
- `%LOCALAPPDATA%/Programs/antigravity/` - Installation directory

See [`PathResolver`](AGRollbackTool/PathResolver.cs) and [`IPathResolver`](AGRollbackTool/IPathResolver.cs) for implementation details.

#### Views

| View | Purpose |
|------|---------|
| `MainWindow` | Primary application window with navigation |
| `DefaultView` | Main operational interface |
| `ErrorStateView` | Error display and recovery options |
| `VerificationDashboard` | Post-rollback verification interface |
| `SettingsPage` | Application settings and preferences |
| `DarkTheme.xaml` | Dark theme resource dictionary |

### Rollback Phases

The full rollback operation executes these phases in sequence:

1. **Backup** - Create timestamped backup of current data
2. **KillProcesses** - Terminate running Antigravity processes
3. **Purge** - Remove installation files and registry entries
4. **FirewallBlackout** - Block network access via Windows Firewall
5. **Install** - Install the target version
6. **ScaffoldCreation** - Create necessary directory structure
7. **Restore** - Restore data from backup
8. **SettingsInjection** - Inject settings including `update.mode=none`
9. **Verification** - Verify all components were restored correctly

## System Requirements

### Minimum Requirements

- **Operating System**: Windows 10 or later (64-bit)
- **Runtime**: .NET 8.0 Runtime
- **Privileges**: Administrator (required for firewall and installation operations)
- **Disk Space**: Minimum 2GB free space for backups

### Supported Antigravity Paths

The tool expects Antigravity to be installed in standard Windows locations:
- Installation: `%LOCALAPPDATA%\Programs\antigravity\`
- User Data: `%USERPROFILE%\.gemini\antigravity\`
- AppData: `%APPDATA%\antigravity\`

## Technology Stack

- **Framework**: WPF (Windows Presentation Foundation)
- **Runtime**: .NET 8.0 (`net8.0-windows`)
- **Language**: C# 12
- **Build**: Self-contained single-file executable
- **Target**: Windows x64

### Key Dependencies

- Windows Firewall API (via `netsh` commands)
- System.Security (for Windows identity)
- System.IO (file operations)
- System.Text.Json (serialization)

## Installation & Build

### Building from Source

1. Ensure .NET 8.0 SDK is installed
2. Clone the repository
3. Navigate to the project directory
4. Build the project:

```bash
cd AGRollbackTool
dotnet build
```

### Running the Application

After building, run the application:

```bash
dotnet run
```

Or execute the compiled binary:

```bash
./AGRollbackTool/bin/Debug/net8.0-windows/win-x64/AGRollbackTool.exe
```

### Publishing

To create a self-contained executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

This produces a single executable at `AGRollbackTool/bin/Release/net8.0-windows/win-x64/publish/AGRollbackTool.exe`.

### Running Tests

```bash
cd AGRollbackTool.Tests
dotnet test
```

## Usage Guide

### Starting the Application

1. Launch the application
2. The application will automatically request administrator privileges if not already running elevated
3. The main interface will display the current installation state

### Creating a Backup

1. From the main view, locate the backup section
2. Click **Create Backup** or **Backup Now**
3. Optionally enable compression for smaller backup files
4. Wait for the backup to complete
5. The backup is stored with a timestamp in the backups folder

Backups include:
- Chat history (`chatHistory.json`)
- User settings (`settings.json`)
- Keybindings (`keybindings.json`)
- Global storage state (`state.vscdb`)
- Agent rules (`GEMINI.md`)

### Performing a Full Rollback

1. Select **Full Rollback** from the main menu
2. Choose the target version to install (or use current version)
3. Configure options:
   - Enable/disable backup before rollback
   - Set phase timeouts (default: 5 minutes)
4. Click **Start Rollback**
5. Monitor progress through each phase
6. Review the verification dashboard when complete

The rollback will:
1. Create a backup of current data
2. Kill all Antigravity processes
3. Purge the installation
4. Block network access
5. Install the target version
6. Restore your data
7. Inject settings (including `update.mode=none`)
8. Verify the installation

### Restore Only

If you have existing backups but Antigravity is not installed:

1. Select **Restore Only** mode
2. Choose a backup from the list
3. Enable hash verification (recommended)
4. Click **Restore**
5. The application will restore all data from the selected backup

### Network Blackout

The network blackout feature blocks Antigravity from accessing the internet:

1. Navigate to **Network Settings** or use the blackout option during rollback
2. Click **Enable Network Blackout**
3. The application creates Windows Firewall rules blocking:
   - `antigravity.exe`
   - `updater.exe`
4. To remove the blackout, click **Remove Firewall Block**

### Verification Dashboard

After rollback or restore, verify the installation:

1. Access the **Verification Dashboard**
2. Click **Run Verification Checks**
3. Review results for:
   - Version confirmation
   - Chat history count
   - GEMINI.md presence
   - Settings restoration (`update.mode=none`)
   - Keybindings file
   - State database integrity
   - Firewall rules status

### Settings

Access settings from the Settings page:

- **Theme**: Toggle between Light and Dark themes
- Theme preference is saved and persists across sessions

## Troubleshooting

### Administrator Privileges Required

The application requires administrator privileges for:
- Windows Firewall management
- Installation/purge operations
- Process termination

If you see an elevation prompt, accept it to continue.

### Backup Not Found

If restoring from a backup fails:
- Verify the backup folder exists
- Check that backup manifest (`manifest.json`) is present
- Ensure SHA-256 hashes match (if verification is enabled)

### Firewall Rules Not Applying

- Verify Windows Firewall is enabled
- Check antivirus is not blocking firewall changes
- Ensure no other security software conflicts

### Installation Not Detected

If the application doesn't detect Antigravity:
- Verify installation path matches expected locations
- Check `%LOCALAPPATA%\Programs\antigravity\` exists
- Ensure `antigravity.exe` is present in the installation directory

## License

This tool is provided for managing your own Antigravity installation. Ensure you comply with Google's terms of service when using this application.

## Contributing

Contributions are welcome. Please ensure:
- Tests pass (`dotnet test`)
- Code follows existing style conventions
- New features include appropriate documentation
