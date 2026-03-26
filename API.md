# AG Rollback Tool - API Reference

This document provides API reference documentation for the AG Rollback Tool, a WPF desktop application for managing backup, restore, and rollback operations for Google's Antigravity AI agent.

## Table of Contents

- [Services](#services)
  - [IRollbackOrchestratorService](#irollbackorchestratorservice)
  - [IBackupService](#ibackupservice)
  - [IPurgeService](#ipurgeservice)
  - [IProcessKiller](#iprocesskiller)
  - [INetworkBlackoutService](#inetworkblackoutservice)
  - [IRestoreService](#irestoreservice)
  - [IAntigravityInstallationService](#iantigravityinstallationservice)
  - [IInstallerVersionService](#iinstallerversionservice)
  - [ISettingsInjectorService](#isettingsinjectorservice)
  - [IVersionDetectorService](#iversiondetectorservice)
  - [IInstallRunnerService](#iinstallrunnerservice)
- [Models](#models)
  - [BackupEntry](#backupentry)
  - [BackupManifest](#backupmanifest)
  - [RollbackSession](#rollbacksession)
  - [VersionInfo](#versioninfo)
  - [AntigravityPathInfo](#antigravitypathinfo)
  - [ProcessInfo](#processinfo)
  - [FirewallOperationResult](#firewalloperationresult)
  - [PurgeResult](#purgeresult)
  - [RestoreResult](#restoreresult)
  - [SettingsChangeResult](#settingschangresult)
  - [RollbackProgress](#rollbackprogress)
  - [RollbackOptions](#rollbackoptions)
  - [RollbackResult](#rollbackresult)
- [Path Resolution](#path-resolution)
  - [IPathResolver](#ipathresolver)
  - [PathResolver](#pathresolver)

---

## Services

### IRollbackOrchestratorService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Coordinates the complete 10-phase rollback pipeline for Antigravity. This is the main orchestrator that sequences all backup, restore, and installation operations.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentPhase` | [`RollbackPhase`](#rollbackphase) | Gets the current phase of the rollback session. |
| `IsInProgress` | `bool` | Gets whether a rollback session is currently in progress. |

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `StartRollbackAsync(RollbackOptions options)` | `Task` | Starts a new rollback session with the specified options. |
| `CancelRollbackAsync()` | `Task` | Cancels the current rollback session. |
| `GetProgress()` | [`RollbackProgress`](#rollbackprogress) | Gets the progress of the current rollback session. |

#### Events

| Event | Event Args | Description |
|-------|------------|-------------|
| `ProgressChanged` | `RollbackProgressChangedEventArgs` | Occurs when the rollback progress changes. |
| `Completed` | `RollbackCompletedEventArgs` | Occurs when the rollback session completes. |
| `ErrorOccurred` | `RollbackErrorEventArgs` | Occurs when the rollback session encounters an error. |

#### Rollback Phases

```csharp
public enum RollbackPhase
{
    NotStarted,           // No rollback session is active
    Backup,              // Performing backup of Antigravity data
    KillProcesses,       // Killing Antigravity processes
    Purge,               // Purging Antigravity installation files
    FirewallBlackout,    // Applying firewall blackout
    Install,             // Installing the specified version
    ScaffoldCreation,    // Creating scaffold files and directories
    Restore,             // Restoring data from backup
    SettingsInjection,   // Injecting settings into restored data
    Verification,        // Verifying the rollback was successful
    Completed,           // Rollback session completed successfully
    Failed,              // Rollback session failed
    Cancelled            // Rollback session was cancelled
}
```

---

### IBackupService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Creates backups of Antigravity data with SHA-256 verification.

#### Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `BackupAsync` | `compress: bool = false` | `Task<string>` | Performs a backup of Antigravity data to a timestamped folder. Returns the path to the backup folder or ZIP file. |

**Exceptions:**
- `IOException` - If an I/O error occurs during backup.
- `UnauthorizedAccessException` - If access to a source or destination path is denied.

**Backed up locations:**
- `%USERPROFILE%\.gemini\antigravity` (directory)
- `%USERPROFILE%\.gemini\GEMINI.md` (file)
- `%APPDATA%\antigravity\User\settings.json` (file)
- `%APPDATA%\antigravity\User\keybindings.json` (file)
- `%APPDATA%\antigravity\User\globalStorage\state.vscdb` (file)

---

### IPurgeService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Removes Antigravity application files from the system.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `PurgeAsync()` | `Task<PurgeResult>` | Purges Antigravity from the system. |

---

### IProcessKiller

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Terminates Antigravity processes.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `KillAntigravity()` | [`ProcessInfo`](#processinfo) | Kills the `antigravity.exe` process. |
| `KillAntigravityHelper()` | [`ProcessInfo`](#processinfo) | Kills the `antigravity-helper.exe` process. |
| `KillAntigravityUtility()` | [`ProcessInfo`](#processinfo) | Kills the `antigravity-utility.exe` process. |
| `KillAntigravityCrashpad()` | [`ProcessInfo`](#processinfo) | Kills the `antigravity-crashpad.exe` process. |
| `KillAllAntigravityProcesses()` | `List<ProcessInfo>` | Kills all known Antigravity processes. |

---

### INetworkBlackoutService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Manages network blackout of Antigravity applications via Windows Firewall.

#### Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `BlockAntigravityNetworkAccess()` | - | `void` | Blocks outbound network access for Antigravity executables. Requires elevated privileges. |
| `UnblockAntigravityNetworkAccess()` | - | `void` | Unblocks network access by removing firewall rules. |
| `RemoveFirewallRule` | `ruleName: string` | [`FirewallOperationResult`](#firewalloperationresult) | Removes a specific firewall rule by name. |
| `VerifyFirewallRule` | `ruleName: string` | `bool` | Verifies if a specific firewall rule exists and is active. |
| `AreFirewallRulesActive()` | - | `bool` | Checks if the firewall rules are active. |

**Blocked executables:**
- `antigravity.exe`
- `updater.exe`

---

### IRestoreService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Restores Antigravity data from backups with SHA-256 verification.

#### Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `RestoreAsync` | `backupPath: string`, `verifyHashes: bool = true` | `Task<RestoreResult>` | Restores data from a backup folder or ZIP file. |

---

### IAntigravityInstallationService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Detects Antigravity installation state and manages edge cases.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `IsAntigravityInstalled()` | `bool` | Checks if Antigravity is currently installed. |
| `GetInstalledVersion()` | `VersionInfo?` | Gets the current version of Antigravity if installed. |
| `HasExistingBackups()` | `bool` | Checks if there are any existing backups. |
| `GetInstallationState()` | [`InstallationState`](#installationstate)` | Gets installation state for UI display. |

#### InstallationState Categories

```csharp
public enum InstallationStateCategory
{
    Normal,       // Antigravity is installed, normal operations can proceed
    NotInstalled, // Antigravity is not installed, nothing to back up
    RestoreOnly,  // Antigravity not installed but backups exist
    Unknown       // Unknown state
}
```

---

### IInstallerVersionService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Validates and compares installer versions.

#### Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `GetInstallerVersion` | `installerPath: string` | `VersionInfo?` | Gets the version from an installer file. |
| `CompareVersion` | `installerPath: string`, `expectedVersion: VersionInfo?` | `VersionComparisonResult` | Compares installer version with expected version. |
| `ShowVersionWarningAndGetConfirmation` | `installerPath: string`, `expectedVersion: VersionInfo?` | `bool` | Shows warning dialog if versions don't match. Returns user's confirmation. |

#### Version Comparison Results

```csharp
public enum VersionComparisonResult
{
    Match,      // Versions match
    Different,  // Installer version differs from expected
    Unknown     // Could not determine installer version
}
```

---

### ISettingsInjectorService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Injects user settings into Antigravity to block updates.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `InjectSettingsAsync()` | `Task<SettingsChangeResult>` | Modifies the Antigravity `settings.json` to block updates. |

**Settings modified:**
- `update.mode` → `"none"`
- `update.showReleaseNotes` → `false`

---

### IVersionDetectorService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Detects the version of Antigravity installed on the system.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `DetectVersion()` | [`VersionInfo`](#versioninfo) | Detects the version of Antigravity. |

**Detection methods (in order):**
1. Read from `package.json` or `product.json` in the installation directory
2. Read from file version info of the main executable
3. Read from Windows Registry

---

### IInstallRunnerService

**Namespace:** `AGRollbackTool.Services`

**Purpose:** Runs the Antigravity installer and manages post-installation steps.

#### Methods

| Method | Parameters | Return Type | Description |
|--------|------------|-------------|-------------|
| `RunInstallationAsync` | `installerPath: string` | `Task<InstallRunnerResult>` | Runs the installation process for Antigravity. |

#### Installation Stages

```csharp
public enum InstallRunnerStage
{
    NotStarted,
    ValidatingInstaller,
    RunningInstaller,
    WaitingForInstallCompletion,
    LaunchingAntigravity,
    VerifyingScaffold,
    Completed
}
```

---

## Models

### BackupEntry

**Namespace:** `AGRollbackTool.Services`

Represents a single file entry in a backup manifest with SHA-256 verification.

| Property | Type | Description |
|----------|------|-------------|
| `RelativePath` | `string` | The relative path of the file within the backup. |
| `OriginalPath` | `string` | The original full path of the file. |
| `Size` | `long` | The size of the file in bytes. |
| `Sha256Hash` | `string` | The SHA-256 hash of the file content. |
| `LastModified` | `DateTime` | The last modified time of the file (UTC). |

---

### BackupManifest

**Namespace:** `AGRollbackTool.Services`

Represents a collection of backup entries with metadata.

| Property | Type | Description |
|----------|------|-------------|
| `Timestamp` | `DateTime` | When the backup was created (UTC). |
| `Entries` | `List<BackupEntry>` | List of backed up files and directories. |
| `IsCompressed` | `bool` | Whether the backup was compressed to a ZIP file. |

---

### RollbackSession

**Namespace:** `AGRollbackTool.Models`

Tracks the state of a rollback session.

| Property | Type | Description |
|----------|------|-------------|
| `CurrentPhase` | `Phase` | The current phase of the session. |
| `StartedAt` | `DateTime` | When the session started (UTC). |
| `EndedAt` | `DateTime?` | When the session ended (UTC), if completed. |
| `ErrorMessage` | `string` | Error message if the session failed. |
| `IsCompleted` | `bool` | Whether the session has completed (success or failure). |
| `IsFailed` | `bool` | Whether the session failed. |

#### Methods

| Method | Parameters | Description |
|--------|------------|-------------|
| `AdvanceTo` | `Phase nextPhase` | Advances the session to a new phase. |
| `SetError` | `string errorMessage` | Marks the session as failed with an error message. |
| `Complete()` | - | Marks the session as completed successfully. |

#### Session Phases

```csharp
public enum Phase
{
    NotStarted,
    Initializing,
    CreatingBackup,
    ApplyingUpdate,
    VerifyingUpdate,
    RollingBack,
    Completed,
    Failed
}
```

---

### VersionInfo

**Namespace:** `AGRollbackTool.Services`

Represents version information (used for version detection results).

| Property | Type | Description |
|----------|------|-------------|
| `Version` | `string` | The version string (e.g., "1.2.3"). |
| `Success` | `bool` | Whether the version was successfully detected. |
| `ErrorMessage` | `string` | Error message if version detection failed. |

**Alternative VersionInfo (InstallerVersionService):**

| Property | Type | Description |
|----------|------|-------------|
| `Major` | `int` | Major version number. |
| `Minor` | `int` | Minor version number. |
| `Build` | `int` | Build number. |
| `FullVersion` | `string` | Complete version string (e.g., "1.2.3"). |

---

### AntigravityPathInfo

**Namespace:** `AGRollbackTool`

Represents resolved path information for Antigravity files and directories.

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string` | The resolved path string. |
| `Exists` | `bool` | Whether the path exists. |
| `ErrorMessage` | `string` | Error message if the path could not be accessed. |

---

### ProcessInfo

**Namespace:** `AGRollbackTool.Services`

Represents information about a terminated process.

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | The process ID. |
| `Name` | `string` | The process name. |
| `IsRunning` | `bool` | Whether the process was running before termination. |
| `ExitCode` | `string?` | The exit code if available. |
| `ErrorMessage` | `string?` | Error message if termination failed. |

---

### FirewallOperationResult

**Namespace:** `AGRollbackTool.Services`

Represents the result of a firewall operation.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the operation was successful. |
| `Message` | `string` | Description of the operation result. |
| `Errors` | `List<string>` | List of errors that occurred. |
| `Warnings` | `List<string>` | List of warnings that occurred. |
| `AffectedRules` | `List<string>` | List of firewall rules affected. |

#### Methods

| Method | Description |
|--------|-------------|
| `HasErrors()` | Returns `true` if there are any errors. |
| `HasWarnings()` | Returns `true` if there are any warnings. |

---

### PurgeResult

**Namespace:** `AGRollbackTool.Services`

Represents the result of a purge operation.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the purge was successful. |
| `DirectoriesDeleted` | `int` | Number of directories deleted. |
| `RegistryKeysDeleted` | `int` | Number of registry keys deleted. |
| `ShortcutsDeleted` | `int` | Number of shortcuts deleted. |
| `Errors` | `List<string>` | List of errors that occurred. |
| `PurgedItems` | `List<string>` | List of items that were purged. |

---

### RestoreResult

**Namespace:** `AGRollbackTool.Services`

Represents the result of a restore operation.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the restore was successful. |
| `FilesRestored` | `int` | Number of files successfully restored. |
| `FilesFailed` | `int` | Number of files that failed to restore. |
| `HashMismatches` | `int` | Number of files with hash mismatches. |
| `Errors` | `List<string>` | List of errors that occurred. |
| `RestoredFiles` | `List<string>` | List of files that were restored. |

---

### SettingsChangeResult

**Namespace:** `AGRollbackTool.Services`

Represents the result of settings injection.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the operation was successful. |
| `Message` | `string` | Description of the outcome. |
| `UpdateModeChanged` | `bool` | Whether `update.mode` was changed. |
| `OldUpdateMode` | `string?` | The old value of `update.mode`. |
| `NewUpdateMode` | `string` | The new value of `update.mode` (default: "none"). |
| `ShowReleaseNotesChanged` | `bool` | Whether `update.showReleaseNotes` was changed. |
| `OldShowReleaseNotes` | `bool?` | The old value of `update.showReleaseNotes`. |
| `NewShowReleaseNotes` | `bool` | The new value (default: `false`). |
| `Errors` | `List<string>` | List of errors encountered. |

---

### RollbackProgress

**Namespace:** `AGRollbackTool.Services`

Represents the progress of a rollback session.

| Property | Type | Description |
|----------|------|-------------|
| `CurrentPhase` | `RollbackPhase` | The current phase. |
| `PercentageComplete` | `int` | Percentage of completion (0-100). |
| `CurrentPhaseDescription` | `string` | Description of the current phase. |
| `ElapsedSeconds` | `int` | Elapsed time in seconds. |
| `EstimatedRemainingSeconds` | `int` | Estimated remaining time in seconds. |
| `CanCancel` | `bool` | Whether the operation can be cancelled. |
| `CurrentStep` | `string` | Current step within the phase. |
| `TotalSteps` | `int` | Total steps in the current phase. |
| `CurrentStepNumber` | `int` | Current step number. |

---

### RollbackOptions

**Namespace:** `AGRollbackTool.Services`

Options for configuring a rollback session.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `CompressBackup` | `bool` | `true` | Whether to compress the backup. |
| `TargetVersion` | `VersionInfo?` | `null` | Target version to install. |
| `SkipBackup` | `bool` | `false` | Skip the backup phase. |
| `SkipKillProcesses` | `bool` | `false` | Skip killing processes phase. |
| `SkipPurge` | `bool` | `false` | Skip the purge phase. |
| `SkipFirewallBlackout` | `bool` | `false` | Skip firewall blackout phase. |
| `SkipInstall` | `bool` | `false` | Skip the install phase. |
| `SkipScaffoldCreation` | `bool` | `false` | Skip scaffold creation phase. |
| `SkipRestore` | `bool` | `false` | Skip the restore phase. |
| `SkipSettingsInjection` | `bool` | `false` | Skip settings injection phase. |
| `SkipVerification` | `bool` | `false` | Skip the verification phase. |
| `PhaseTimeoutSeconds` | `int` | `300` | Timeout for each phase in seconds. |

---

### RollbackResult

**Namespace:** `AGRollbackTool.Services`

Represents the result of a complete rollback operation.

| Property | Type | Description |
|----------|------|-------------|
| `Success` | `bool` | Whether the rollback was successful. |
| `BackupPath` | `string` | Path to the backup if one was created. |
| `InstalledVersion` | `VersionInfo` | Version that was installed. |
| `TotalTimeSeconds` | `int` | Total time taken in seconds. |
| `ErrorMessage` | `string` | Error message if rollback failed. |
| `CompletedPhases` | `List<RollbackPhase>` | Phases that completed successfully. |
| `FailedPhases` | `List<RollbackPhase>` | Phases that failed. |

---

## Path Resolution

### IPathResolver

**Namespace:** `AGRollbackTool`

**Purpose:** Interface for resolving Antigravity-related paths.

#### Methods

| Method | Return Type | Description |
|--------|-------------|-------------|
| `GetGeminiAntigravityPath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the raw chat JSON path (`%USERPROFILE%\.gemini\antigravity`). |
| `GetGeminiGlobalRulesPath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the global agent rules path (`%USERPROFILE%\.gemini\GEMINI.md`). |
| `GetUserSettingsPath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the user settings path (`%APPDATA%\antigravity\User\settings.json`). |
| `GetUserKeybindingsPath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the user keybindings path (`%APPDATA%\antigravity\User\keybindings.json`). |
| `GetGlobalStorageStatePath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the global storage state path (`%APPDATA%\antigravity\User\globalStorage\state.vscdb`). |
| `GetApplicationBinaryPath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the application binary path (`%LOCALAPPDATA%\Programs\antigravity`). |
| `GetStagedUpdateCachePath()` | [`AntigravityPathInfo`](#antigravitypathinfo) | Gets the staged update cache path (`%LOCALAPPDATA%\antigravity-updater`). |
| `GetAllAntigravityPaths()` | `IEnumerable<AntigravityPathInfo>` | Gets all Antigravity paths. |

---

### PathResolver

**Namespace:** `AGRollbackTool`

**Purpose:** Implementation of `IPathResolver` that resolves Antigravity paths on the local system.

**Implementation Details:**
- `PathResolver` implements `IPathResolver`
- Each method returns an `AntigravityPathInfo` object with:
  - `Path`: The resolved path string
  - `Exists`: Whether the path exists (file or directory)
  - `ErrorMessage`: Any error encountered while resolving the path

**Resolved Paths:**

| Path Type | Environment Path |
|-----------|------------------|
| Chat Data | `%USERPROFILE%\.gemini\antigravity` |
| Global Rules | `%USERPROFILE%\.gemini\GEMINI.md` |
| User Settings | `%APPDATA%\antigravity\User\settings.json` |
| User Keybindings | `%APPDATA%\antigravity\User\keybindings.json` |
| Global Storage | `%APPDATA%\antigravity\User\globalStorage\state.vscdb` |
| Application Binary | `%LOCALAPPDATA%\Programs\antigravity` |
| Update Cache | `%LOCALAPPDATA%\antigravity-updater` |

---

*Document generated for AG Rollback Tool API Reference*
