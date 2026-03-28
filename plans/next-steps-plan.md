# AG Rollback Tool - Next Steps Plan

## Project Overview

The AG Rollback Tool is a WPF desktop application for managing backup, restore, and rollback operations for Google's Antigravity AI agent on Windows. The project is well-developed with:
- 25+ service classes covering backup, restore, purge, process killing, network blackout, installation, and verification
- 12+ test files covering main services
- Full WPF UI implementation
- Documentation (API.md and README.md)

## Identified Gaps and Improvements

### 1. Missing Logging Infrastructure
- **Current State**: No logging framework (no Serilog, NLog, or similar)
- **Impact**: Difficult to debug production issues, no audit trail
- **Priority**: HIGH

### 2. Missing Configuration Management
- **Current State**: Uses basic .NET ConfigurationManager, no structured appsettings.json
- **Impact**: Hardcoded timeouts, paths, and settings throughout codebase
- **Priority**: HIGH

### 3. Test Coverage Gaps
- **Services without dedicated tests**:
  - `AntigravityInstallationService` (detects installation status)
  - `InstallerVersionService` (validates installer versions)
  - `PathResolver` / `IPathResolver` (path resolution)
- **Priority**: MEDIUM

### 4. No Dependency Injection Container
- **Current State**: Manual instantiation of services
- **Impact**: Tight coupling, harder to test, no lifecycle management
- **Priority**: MEDIUM

### 5. Missing Integration with System Tray
- **Current State**: Standard WPF window
- **Impact**: No background operation support
- **Priority**: LOW

---

## Prioritized Next Steps

### Phase 1: Foundation Improvements (Do First)

| # | Task | Description | Priority |
|---|------|-------------|----------|
| 1.1 | Add Logging | Integrate Serilog with file sink for production debugging | HIGH |
| 1.2 | Create appsettings.json | Externalize timeouts, paths, backup locations | HIGH |
| 1.3 | Add DI Container | Add Microsoft.Extensions.DependencyInjection | MEDIUM |

### Phase 2: Test Coverage (Do Second)

| # | Task | Description | Priority |
|---|------|-------------|----------|
| 2.1 | Test AntigravityInstallationService | Add tests for installation detection | MEDIUM |
| 2.2 | Test InstallerVersionService | Add tests for version validation | MEDIUM |
| 2.3 | Test PathResolver | Add tests for path resolution | MEDIUM |

### Phase 3: Enhancements (Do Third)

| # | Task | Description | Priority |
|---|------|-------------|----------|
| 3.1 | Add System Tray Support | Minimize to tray, background operations | LOW |
| 3.2 | Add Global Exception Handler | Catch unhandled exceptions, log and show error dialog | MEDIUM |
| 3.3 | Add Telemetry/Analytics | Anonymous usage tracking for improvement | LOW |

---

## Implementation Approach

### 1. Logging Setup (Serilog)

```csharp
// Add to AGRollbackTool.csproj:
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

// Initialize in App.xaml.cs:
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/agrollback-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

### 2. Configuration (appsettings.json)

```json
{
  "BackupSettings": {
    "BackupDirectory": "%APPDATA%\\AGRollbackTool\\Backups",
    "MaxBackups": 10
  },
  "TimeoutSettings": {
    "PhaseTimeoutMinutes": 5,
    "InstallTimeoutMinutes": 15
  },
  "AntigravityPaths": {
    "Installation": "%LOCALAPPDATA%\\Programs\\antigravity",
    "UserData": "%USERPROFILE%\\.gemini\\antigravity"
  }
}
```

### 3. Dependency Injection

```csharp
// In App.xaml.cs startup:
services.AddSingleton<IBackupService, BackupService>();
services.AddSingleton<IRestoreService, RestoreService>();
// ... etc
```

---

## Decision Points

1. **Logging Framework**: Serilog vs NLog - Serilog is more common in .NET Core/8 projects
2. **DI Container**: Microsoft.Extensions.DependencyInjection is built-in to .NET 8
3. **Configuration Format**: JSON appsettings vs custom XML - JSON is standard

---

## Risks and Considerations

1. **Breaking Changes**: Adding DI may require refactoring service constructors
2. **Single-File Publish**: Serilog file sink works with single-file, but need to ensure log directory is writable
3. **Test Migration**: Adding tests for missing services requires understanding their behavior

---

## Success Metrics

- All services have at least basic unit tests
- All critical operations logged with appropriate levels (Debug, Info, Warning, Error)
- Configuration externalized so non-code changes don't require recompilation
- Application builds and runs in Release mode as single-file executable
