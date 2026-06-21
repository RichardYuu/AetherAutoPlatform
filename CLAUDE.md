# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Aether Device Software Platform** — A C# .NET Framework 4.7.2 + WinForms industrial automation HMI platform supporting 13 optical equipment types. Features a six-layer architecture (UI → Orchestration → Business → Device → HAL → Data), dual-engine design (Lua scripting + workflow canvas), hardware simulation mock layer, and three-mode database strategy.

---

## Common Commands

### Build & Run
```bash
# Open solution in Visual Studio
start Aether.Platform.sln

# Restore NuGet packages (if missing)
nuget restore Aether.Platform.sln
# Or in VS: Right-click Solution → Restore NuGet Packages

# Build solution
msbuild Aether.Platform.sln /p:Configuration=Debug
# Or in VS: Build → Build Solution (Ctrl+Shift+B)

# Run: Set Aether.Platform.App as startup project, press F5
```

### Key Development Commands
```bash
# Check project structure
ls Aether.Platform.Core/ Interfaces/ Models/ Utilities/
ls Aether.Platform.Services/ AllServices.cs
ls Aether.Platform.Business/ AllBusiness.cs
ls Aether.Platform.Devices/ AllDevices.cs
ls Aether.Platform.HAL/ AllHAL.cs
ls Aether.Platform.Data/ AllData.cs
ls Aether.Platform.UI/ Shell/ Modules/
ls Aether.Platform.Scripting/ AllScripting.cs
ls Aether.Platform.Workflow/ AllWorkflow.cs

# Search for interface definitions
grep -r "interface I" Aether.Platform.Core/Interfaces/

# Find service implementations
grep -r "class.*Service" Aether.Platform.Services/
```

---

## Architecture Overview

### Six-Layer Architecture
```
UI Layer (WinForms)          → MainShellForm, Modules, Controls, Login
Orchestration Layer          → LuaScriptEngine + WorkflowEngine (Phase 2)
Business Logic Layer         → Production, Quality, Traceability, Maintenance...
Device Adaptation Layer      → 13 devices + DeviceFactory (PlcBased/DirectControl)
HAL (Hardware Abstraction)   → Real + Sim dual implementation
Data Access Layer            → 3-mode DB + IFMS + DataCollection + Repository
```

### Solution Structure (10 Projects)
```
Aether.Platform.sln
├── Aether.Platform.Core           # Interfaces, Models, Utilities, ServiceLocator
├── Aether.Platform.App            # Entry (Program.cs + AppBootstrap DI registration)
├── Aether.Platform.UI             # WinForms Shell + 13 modules + MVP
├── Aether.Platform.Services       # 11 base services (State, Parameter, Config, Alarm...)
├── Aether.Platform.Business       # 7 business services (Production, Quality, SPC...)
├── Aether.Platform.Devices        # 13 device adapters + DeviceFactory
├── Aether.Platform.HAL            # Hardware abstraction (Real + Sim + Peripherals)
├── Aether.Platform.Data           # DB providers, IFMS, Repository, Config
├── Aether.Platform.Scripting      # MoonSharp Lua engine + Hardware bindings
└── Aether.Platform.Workflow       # GDI+ workflow canvas + engine
```

### Dependency Flow
```
Core ← All projects reference Core (interface/model layer)
Services ← Core + Data
Business ← Core + Services
Devices ← Core + HAL
HAL ← Core
Data ← Core
App ← All projects (DI container builder)
UI ← Core + Services + Business + Devices + HAL + Data
```

---

## Core Concepts

### ServiceLocator (MS DI Wrapper)
```csharp
// Get service
var state = ServiceLocator.GetService<IStateService>();

// Register (in AppBootstrap after Initialize)
ServiceLocator.RegisterSingleton<IAuthService>(new AuthService());

// ParameterService gets DbContext via DI injection (shared instance)
public ParameterService(DbContext dbContext = null) { ... }
```

### Hardware Abstraction (HAL)
- **Real mode**: KeyencePLC, HuichuanPLC, LeadshineMotion, Basler/HIK cameras
- **Sim mode**: SimulatedAxis, SimulatedPLC, SimulatedCamera, SimulatedScanner
- **4 simulation modes**: None / Full / Partial / Replay
- **14 hardware interfaces**: IAxis, IDigitalIO, IVision, IPlcCommunication, IBarcodeScanner, ISerialPort, IModbusCommunication, IAnalogIO, IScaleDevice, IHeightGauge, ITemperatureController, IMicropressureSensor, IElectricProportionalValve, IExposureMeter, ISpotAnalyzer

### Database Three-Mode Strategy
- **SqlServerOnly**: Pure SQL Server for networked lines with DBA
- **AccessOnly**: Pure Access for offline single-machine devices
- **SqlServerWithAccessFallback**: SQL Server primary + Access failover with auto-switch

### Device Factory (13 Devices)
`MTFTest`, `ZG13`, `XL07`, `PX09`, `DJ10`, `ZZ14`, `BB08`, `DJ16`, `SF16`, `DB06`, `CC03`, `AssemblyNew`, `Assembly`

Two device base classes:
- `PlcBasedDevice`: Motion/IO controlled by PLC (Keyence/Huichuan)
- `DirectControlDevice`: All hardware directly controlled by PC

### Shell UI Architecture
- **Single window**: MainShellForm with Header(64px) + SimPanel(28px) + Sidebar(40px) + BottomTab(42px) + ContentPanel
- **Cache pool**: Modules loaded once, hidden/shown (no rebuild)
- **MVP pattern**: Each module UserControl ↔ Presenter pair
- **Keyboard shortcuts**: F1=Main, F2=StatusLog, F3=ControlDebug, F4=VisionDebug, F5=ProcessDebug, F6=SystemConfig, F8=E-Stop
- **Three-color indicator**: 🟢Running / 🟡Standby&Debug / 🔴Alarm&Stop

### Multi-language (i18n)
- Resources: `Strings.resx` (zh-CN), `Strings.en.resx`, `Strings.vi.resx`
- Service: `ILocalizationService.T(key)` for translation
- Runtime switch with culture change + ComponentResourceManager.ApplyResources()

---

## Key Files to Know

| File | Purpose |
|------|---------|
| `Aether.Platform.App/Program.cs` | Entry point, sets RuntimeMode, loads device |
| `Aether.Platform.App/AppBootstrap.cs` | DI container setup, registers all services, builds IServiceProvider |
| `Aether.Platform.Core/Utilities/ServiceLocator.cs` | Global service locator (MS DI wrapper) |
| `Aether.Platform.Core/Interfaces/IModuleView.cs` | View interface for all UI modules |
| `Aether.Platform.UI/Shell/MainShellForm.cs` | Main shell window, navigation, layout |
| `Aether.Platform.UI/Shell/NavigationManager.cs` | Module cache pool navigation |
| `Aether.Platform.Services/AllServices.cs` | 11 service implementations |
| `Aether.Platform.Business/AllBusiness.cs` | 7 business service implementations |
| `Aether.Platform.Devices/AllDevices.cs` | 13 device implementations + DeviceFactory |
| `Aether.Platform.HAL/AllHAL.cs` | HAL interfaces + Real/Sim implementations |
| `Aether.Platform.Data/AllData.cs` | DB providers, IFMS, Repository, Config |
| `Aether.Platform.Scripting/AllScripting.cs` | Lua engine + HardwareBindings + GlobalsRegistry |
| `Aether.Platform.Workflow/AllWorkflow.cs` | WorkflowEngine + Canvas + Builder |

---

## Thread Model (8 Threads + 1 Queue + UI Marshaling)

| Thread | Purpose | Cycle | Safety |
|--------|---------|-------|--------|
| UI (STA) | WinForms controls + 100ms refresh | Continuous | Only thread that touches controls |
| FlowRunner | 300+ auto flow execution | Continuous | volatile + lock(_stepLock) |
| SafetyMonitor | Safety signal polling (50ms/100ms) | High-frequency | Independent, no shared lock with FlowRunner |
| HardwareStatePoller | PLC tags + axis position → memory | 200ms | ReaderWriterLockSlim |
| IfmsUploadWorker | BlockingCollection → HTTP upload | Continuous | BlockingCollection built-in safety |
| IfmsHeartbeat | Device online heartbeat | 60s | Read-only + HTTP |
| TimedTaskScheduler | 7 timed tasks | Scheduled | Independent CTS |

**Thread Communication**:
- Backend → UI: `SynchronizationContext.Post()`
- UI → Backend: `Task.Run()` / `BlockingCollection.Enqueue()`
- Shared state: `ReaderWriterLockSlim` + `volatile`

---

## Adding New Components

### Add a New UI Module
1. Create `UserControl` implementing `IModuleView` in `UI/Modules/`
2. Create corresponding `Presenter` class
3. Register in `MainShellForm.BuildBottomTabBar()` tabs array
4. Register module factory in `NavigationManager`

### Add a New Service
1. Define interface in `Core/Interfaces/Services/`
2. Implement in `Services/` project
3. Register in `AppBootstrap.RegisterAllServices()` via `_services.AddSingleton<T>()`
4. Access via `ServiceLocator.GetService<T>()`

### Add Data Persistence (Repository Pattern)
1. Define `IMyRepository` interface in `Core/Interfaces/Services/IDataRepository.cs`
2. Implement in `Data/DataRepository.cs`
3. Register in `AppBootstrap` (step [1/5] Register Data Layer)
4. Inject via constructor in target Service

### Add New Device
1. Create device class inheriting `PlcBasedDevice` or `DirectControlDevice`
2. Register in `DeviceFactory` creators dictionary
3. Configure in `appsettings.json`

---

## Important Design Rules

1. **UI never blocks**: All heavy operations must be offloaded to background threads
2. **Safety independent path**: SafetyMonitor thread doesn't share locks with FlowRunner
3. **Data loss prevention**: Critical data double-written (DB + JSON), state snapshot on power-off
4. **Concurrency control**: Hardware operations use `SemaphoreSlim(1)`, alarms use `ConcurrentQueue`, files use `lock`/NLog async queue
5. **No MDI windows**: Single Shell window with embedded UserControl modules
6. **All UI built in code**: No WinForms designer drag-and-drop
7. **Lock ordering**: All threads must acquire locks in consistent order to prevent deadlocks
8. **CancellationToken propagation**: All blocking async methods must accept CancellationToken

---

## Related Documentation

- `../平台化软件开发方案.md` — Full technical solution (v3.6)
- `../README-快速开始.md` — Quick start guide with detailed configuration
- `../代码架构说明.md` — Detailed code architecture explanation (C# beginner-friendly)
- `../自动化平台-流程引擎开发方案.md` — Lua scripting + workflow canvas design
