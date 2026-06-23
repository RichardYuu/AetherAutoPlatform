> **本项目由 [OpenCode](https://github.com/obytes/open-code) 进行提炼重构。**  
> OpenCode 是一款强大的 AI 编程助手，全程参与了该平台的代码重构、架构梳理与质量提升。向 OpenCode 致敬！🚀
> ClaudeCode 参与整理；

# Aether 设备软件平台 (Aether Device Software Platform)

> **工控自动化上位机软件平台** — 基于 C# .NET Framework 4.7.2 + WinForms，支撑 Aether 公司 13 种光学设备。

| 属性 | 值 |
|------|-----|
| **框架** | .NET Framework 4.7.2 / C# 7.3 |
| **UI** | WinForms (Shell+UserControl, 单窗口架构) |
| **DI 容器** | Microsoft.Extensions.DependencyInjection |
| **ORM** | Dapper 2.1.28 |
| **日志** | NLog 5.2.0 |
| **脚本引擎** | MoonSharp 2.15.0 (Lua 5.2) |
| **数据库** | SQL Server 2016+ / Access 2016+ (.accdb) |
| **PLC** | 基恩士 KV 系列 / 汇川 H5U/EASY/AM600 |
| **运动控制** | 雷赛 DMC/EtherCAT / 固高 GTS / 正运动 ZMC |

---

## 📋 目录

- [项目概览](#项目概览)
- [解决方案结构](#解决方案结构)
- [六层分层架构](#六层分层架构)
- [技术亮点](#技术亮点)
- [项目模块说明](#项目模块说明)
- [硬件抽象层 (HAL)](#硬件抽象层-hal)
- [硬件仿真 Mock 层](#硬件仿真-mock-层)
- [数据库三模式策略](#数据库三模式策略)
- [设备清单 (13 种)](#设备清单-13-种)
- [22 项业务功能](#22-项业务功能)
- [基础服务架构](#基础服务架构)
- [双引擎架构 (第二阶段)](#双引擎架构-第二阶段)
- [Shell UI 架构](#shell-ui-架构)
- [多语言支持 (i18n)](#多语言支持-i18n)
- [自动流程控制](#自动流程控制)
- [技术栈](#技术栈)
- [环境要求与构建](#环境要求与构建)
- [快速开始](#快速开始)

---

## 项目概览

Aether 设备软件平台是一套 **平台化、模块化的工控上位机软件框架**，解决传统工控项目"每个设备单独开发、代码复用率低、开发周期长"的核心痛点。

### 设计目标

| 目标 | 指标 |
|------|------|
| 新设备开发周期 | ≤2 周 |
| 代码复用率 | 70%+ |
| 支持设备类型 | 13 种 |
| 硬件抽象接口 | 14 类（轴、IO、相机、PLC、扫码器、传感器等）|
| 仿真模式 | 4 种（Full / Partial / Replay / None）|
| 数据库策略 | 3 种可配置（单 SQL Server / 单 Access / 主备容灾）|
| 多语言 | zh-CN / en / vi-VN |

---

## 解决方案结构

```
Aether.Platform.sln
├── Aether.Platform.App          # 应用入口 (Program.cs + AppBootstrap)
├── Aether.Platform.Core         # 核心层 (接口、模型、工具类、ServiceLocator)
├── Aether.Platform.Services     # 基础服务 (11 个独立服务)
├── Aether.Platform.Business     # 业务逻辑 (7 个业务服务)
├── Aether.Platform.Devices      # 设备适配 (13 种设备 + DeviceFactory)
├── Aether.Platform.HAL          # 硬件抽象层 (真实 + 仿真双实现)
├── Aether.Platform.Data         # 数据访问层 (三模式 DB + IFMS 数据采集)
├── Aether.Platform.Scripting    # Lua 脚本引擎 (第二阶段)
├── Aether.Platform.Workflow     # 工作流引擎 (第二阶段)
└── Aether.Platform.UI           # 界面层 (WinForms + MVP + 缓存池)
```

---

## 六层分层架构

```
┌─────────────────────────────────────────────────┐
│               UI 表现层 (WinForms)               │
│   MainShellForm · Modules · Controls · Login     │
├─────────────────────────────────────────────────┤
│              服务编排层 (Services)                │
│   State · Parameter · Config · Alarm · Auth ·   │
│   Audit · Init · FlowProperty · FlowRunner ·    │
│   TimedTask · Localization                       │
├─────────────────────────────────────────────────┤
│              业务逻辑层 (Business)                │
│   Production · GoldenSample · CheckList ·        │
│   Quality · Traceability · Maintenance · Export  │
├─────────────────────────────────────────────────┤
│              设备适配层 (Devices)                 │
│   13 种设备 · DeviceBase · DeviceFactory          │
│   PlcBasedDevice / DirectControlDevice           │
├─────────────────────────────────────────────────┤
│            硬件抽象层 HAL (Real + Sim)            │
│   IAxis · IDigitalIO · IVision · IPlcComm ·     │
│   IScanner · ISerialPort · IModbus · IAnalogIO   │
│   + 7 种外设接口                                │
├─────────────────────────────────────────────────┤
│              数据访问层 (Data)                    │
│   SqlServerProvider · AccessProvider ·           │
│   FailoverProvider · IFMS 采集 · DataCollection  │
└─────────────────────────────────────────────────┘
```

---

## 技术亮点

### 1. 统一依赖注入 (DI)

基于 `Microsoft.Extensions.DependencyInjection`，所有服务统一注册、解析：

```csharp
// AppBootstrap 启动时注册
_services.AddSingleton<StateService>();
_services.AddSingleton<ParameterService>();
_services.AddSingleton<AlarmService>();

// 任意位置通过 ServiceLocator 获取
var state = ServiceLocator.GetService<IStateService>();
```

### 2. 兼容旧版 ServiceLocator 静态 API

在 MS DI 之上保留 `ServiceLocator.GetService<T>()` 静态接口，同时支持运行时动态注册：

```csharp
ServiceLocator.RegisterSingleton<IIfmsBroker>(new IfmsHttpClient());
```

### 3. 硬件仿真 Mock 层 — 无硬件也能开发

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| `None` | 完全使用真实硬件 | 产线正式运行 |
| `Full` | 全部硬件仿真 | 初期开发、UI 调试、逻辑验证 |
| `Partial` | 部分仿真 + 部分真实 | 已有部分硬件时的渐进式联调 |
| `Replay` | 回放已录制的真实数据 | 复现产线问题、回归测试 |

```csharp
// Program.cs 启动模式选择
var bootstrap = new AppBootstrap { Mode = RuntimeMode.Simulation };
```

### 4. 三模式可配置数据库策略

每个设备独立配置数据库策略：

- **SqlServerOnly** — 纯 SQL Server，有 DBA 的网络产线
- **AccessOnly** — 纯 Access，无网络单机设备
- **SqlServerWithAccessFallback** — SQL Server 主 + Access 备，高可用产线，故障自动切换

### 5. 双引擎架构 (第二阶段)

| 引擎 | 技术 | 用途 |
|------|------|------|
| **Lua 脚本引擎** | MoonSharp 2.15.0 | 脚本化控制，无需重新编译主程序 |
| **工作流画布** | GDI+ 自研 | 类 VisionMaster 拖拽式可视化流程编排 |

---

## 项目模块说明

### Aether.Platform.App — 应用入口

应用程序启动入口，包含 `Program.cs`（Main 方法）和 `AppBootstrap`（初始化引导器）。

```csharp
// Program.cs — 启动流程
static void Main()
{
    var bootstrap = new AppBootstrap { Mode = RuntimeMode.Simulation };
    bootstrap.Initialize();          // 初始化 DI、HAL、服务、脚本引擎
    bootstrap.LoadDevice("MTFTest"); // 加载指定设备
    Application.Run(new MainShellForm());
}
```

`AppBootstrap` 负责：
1. 创建 `ServiceCollection` 并注册所有服务（数据层 → 核心服务 → HAL → 业务层 → 脚本引擎）
2. 构建 `IServiceProvider`（启用 `validateOnBuild`）
3. 注入到全局 `ServiceLocator`
4. 注册硬件绑定到 Lua 引擎
5. 启动后台定时任务
6. 加载设备并自动生成工作流

### Aether.Platform.Core — 核心层

包含所有核心接口定义、数据模型、枚举、工具类。

**接口定义：**
- `IHardwareService` — 统一硬件服务接口（14 类硬件）
- `IDevice` — 设备接口
- `IBusinessServices` — 业务服务接口
- `IDataRepository` — 数据仓储接口
- `IStateService` / `IParameterService` 等 — 服务接口
- `AllHALInterfaces` — HAL 层全部子接口（IAxis, IDigitalIO, IVision, IPlcCommunication 等）

**模型：**
- `Enums.cs` — MachineStatus, DeviceStatus, AlarmLevel, DatabaseMode, FlowState, TrayState, UserRole 等
- `DataModels.cs` — FlowAction, ParameterChangeLog, LoginResult, StationValidation, ProductionDataUpload, QualityReportData, TrayStateMachine, LocalizedString 等
- `ResultModels.cs` — 通用返回结果
- `SimDataModels.cs` — 仿真数据模型

**工具类：**
- `ServiceLocator` — 全局服务定位器（MS DI 封装）
- `EncryptionHelper` — 加密辅助

### Aether.Platform.Services — 基础服务层

共 **11 个独立服务**，替代传统单体 `G_ClassStatic`：

| 服务 | 职责 | 持久化 |
|------|------|--------|
| `StateService` | 运行时状态（机台状态、权限、件号、点检）+ 断电快照 | 内存 + JSON 快照 |
| `ParameterService` | 设备/工艺参数 CRUD（三模式持久化） | 按配置选择 |
| `ConfigurationService` | 应用/硬件配置加载与热重载 | JSON 文件 |
| `AlarmService` | 报警触发/清除（Error/Tip/Trace 三级）+ 处理建议 | 事件 + DB |
| `AuthService` | 登录认证（密码/刷卡/指纹/U 盘狗/IFMS） | DB |
| `AuditService` | 操作审计（按钮/参数/登录，不可删除） | DB |
| `InitializationService` | 硬件/参数分级报错，崩溃异常处理 | — |
| `FlowPropertyService` | 流程属性字典（-1 初始化 → 300+ 自动运行） | 内存 |
| `FlowRunnerService` | 自动流程控制（线程管理、暂停/停止/复位/急停） | 运行时 |
| `TimedTaskSchedulerService` | 定时后台任务（7 项） | — |
| `LocalizationService` | 多语言切换（zh-CN / en / vi-VN） | .resx |

### Aether.Platform.Business — 业务逻辑层

共 **7 个业务服务**：

| 服务 | 功能 |
|------|------|
| `ProductionService` | 生产记录管理（OK/NG/未测试） |
| `GoldenSampleService` | 封样管理（20+ 封样记录、自动封样、平均值计算） |
| `CheckListService` | 点检管理（开机/换件号强制点检） |
| `QualityService` | 品质分析 SPC（CPK/PPK/X-R 图表、预警停机） |
| `TraceabilityService` | 追溯管理（前后道数量核对、产品工站校验） |
| `MaintenanceService` | 维保管理（自定义频率、轴/气动件次数追踪） |
| `ExportService` | 数据导出（按天自动导出 CSV/Excel） |

所有业务服务通过构造函数注入 `IDataRepository` 接口（本地 JSON 持久化），无需关心底层存储实现。

### Aether.Platform.Devices — 设备适配层

**设备基类层次：**

```
DeviceBase (抽象基类)
├── PlcBasedDevice    — 类型一：PLC 控制模式（运动/IO 由 PLC 控制）
└── DirectControlDevice — 类型二：上位机直控模式（全部硬件上位机控制）
```

**DeviceFactory — 工厂模式创建设备：**

```csharp
var device = DeviceFactory.Create("MTFTest");
```

**已注册设备 (13 种)：** `MTFTest`, `ZG13`, `XL07`, `PX09`, `DJ10`, `ZZ14`, `BB08`, `DJ16`, `SF16`, `DB06`, `CC03`, `AssemblyNew`, `Assembly`

每个设备目录包含独立的 `*Device.cs` 实现类。

**`HalBinding.cs`** — 设备到 HAL 的绑定映射，连接设备逻辑与硬件操作。

**`DeviceStep.cs`** — 设备工步定义，用于自动生成工作流。

### Aether.Platform.HAL — 硬件抽象层

**体系结构：**

```
IHardwareService (统一接口)
├── Adapters/
│   ├── PlcBasedAdapter       — 类型一：PLC 控制适配器
│   └── DirectControlAdapter   — 类型二：直接控制适配器
├── Real/                     — 真实硬件实现
│   ├── RealHardwareService    — 真实硬件服务实现
│   ├── KeyencePLC.cs          — 基恩士 KV 系列 PLC
│   ├── HuichuanPLC.cs         — 汇川 H5U/EASY/AM600 PLC
│   ├── CameraBasler.cs        — Basler 相机
│   ├── CameraHIK.cs           — 海康威视相机
│   ├── BarcodeScannerDevice.cs — 扫码器
│   ├── DigitalIOController.cs — 数字 IO 控制器
│   ├── LeadshineMotion.cs     — 雷赛运动控制器
│   └── SerialPortDevice.cs    — 串口通信
├── Sim/                      — 仿真硬件实现
│   ├── SimulatedHardwareService — 仿真硬件服务
│   ├── SimulatedAxis           — 仿真轴（运动插值、限位触发、噪声注入）
│   ├── SimulatedPLC            — 仿真 PLC（寄存器读写、自动更新、急停信号）
│   ├── SimulatedCamera         — 仿真相机（本地图片素材、可调定位分数）
│   ├── SimulatedScanner        — 仿真扫码器（预置码队列、可调成功率）
│   ├── SimulatedDigitalIO      — 仿真数字 IO
│   ├── SimulatedAnalogIO       — 仿真模拟量 IO
│   ├── SimulatedSerialPort     — 仿真串口
│   ├── SimulatedModbus         — 仿真 Modbus
│   └── + 8 种外设仿真
├── Peripherals/               — 专用外设
│   ├── HeightGaugeDevice       — 测高仪
│   ├── TemperatureControllerDevice — 温控器
│   ├── MicropressureSensorDevice   — 微压计
│   ├── EPValveDevice           — 电气比例阀
│   ├── ExposureMeterDevice     — 曝光计
│   ├── SpotAnalyzerDevice      — 光斑仪
│   └── ScaleDevice             — 称重设备
└── HardwareServiceFactory.cs   — 硬件服务工厂
    HardwareServiceMode.cs      — 模式枚举
```

**支持硬件接口 (14 类)：**

| 接口 | 用途 |
|------|------|
| `IAxis` | 伺服/步进轴控制（绝对/相对运动、回原点、启停） |
| `IDigitalIO` | 数字 IO 点读写 |
| `IAnalogIO` | 模拟量输入输出 |
| `IPlcCommunication` | PLC 寄存器/线圈读写 |
| `IBarcodeScanner` | 扫码器 |
| `IVisionSystem` | 视觉系统（拍照/定位/匹配） |
| `ISerialPort` | 串口通信 (RS232/RS485) |
| `IModbusCommunication` | Modbus RTU/TCP |
| `IScaleDevice` | 称重设备 |
| `IHeightGauge` | 测高仪 |
| `ITemperatureController` | 温控器 |
| `IMicropressureSensor` | 微压计 |
| `IElectricProportionalValve` | 电气比例阀 |
| `IExposureMeter` | 曝光计 |
| `ISpotAnalyzer` | 光斑仪 |

### Aether.Platform.Data — 数据访问层

**数据库三模式架构：**

```
DatabaseProviderFactory
├── SqlServerProvider    — SQL Server 2016+ 连接
├── AccessProvider       — Access .accdb 连接
└── FailoverProvider     — SqlServer 主 + Access 备（自动故障切换与恢复）
```

**核心类：**

| 类 | 描述 |
|------|------|
| `DatabaseProviderFactory` | 根据配置创建对应模式的 DatabaseProvider |
| `SqlServerProvider` | SQL Server 数据库操作（Dapper） |
| `AccessProvider` | Access 数据库操作 |
| `FailoverProvider` | 双写 + 故障自动切换，主恢复后自动回切 |
| `DbContext` | 数据库上下文（DI 注入） |

**IFMS 数据采集：**

| 类 | 描述 |
|------|------|
| `IfmsHttpClient` | IFMS HTTP 通信客户端 |
| `IfmsUploadQueue` | 离线缓冲队列（断网时存 DB，恢复后 FIFO 上传） |
| `DataRepository` | 数据仓储基类 |
| `DeviceStatusCollector` | 设备状态采集（心跳 60 秒） |
| `ProductionDataCollector` | 生产数据采集 |
| `QualityDataCollector` | 品质数据采集（CPK/PPK/X-R） |

**配置管理：**

| 类 | 描述 |
|------|------|
| `AppConfig` | 应用配置数据模型 |
| `ConfigManager` | JSON 配置文件加载/保存（热重载支持） |

### Aether.Platform.Scripting — Lua 脚本引擎 (第二阶段)

基于 **MoonSharp 2.15.0** 的 Lua 5.2 脚本引擎，支持：

- **完整 Lua 语法** — 变量、函数、表、循环、条件
- **硬件函数绑定** — 28 个委托（轴运动、IO、PLC、扫码、相机、传感器）
- **行级断点调试** — 基于 MoonSharp 原生 `IDebugger` 接口实现
- **单步执行** — StepOver 模式
- **变量监视** — 暂停时查看局部变量快照
- **全局变量同步** — `GlobalsRegistry` 管理 Lua ↔ C# 共享状态

```lua
-- Lua 脚本示例
local barcode = scan_barcode("SCANNER_1", 5000)
globals:set("CurrentBarcode", barcode)
axis_move_abs("X", 100.0, 50.0)
axis_wait_done("X", 10000)
io_set("DO_LAMP", true)
local score = vision_locate("CAM_1", "template_a")
return true  -- 完成分支
```

**核心类：**

| 类 | 描述 |
|------|------|
| `LuaScriptEngine` | 脚本引擎（加载、运行、暂停、停止、调试） |
| `HardwareBindings` | 硬件函数注册（轴/IO/PLC/相机/扫码/传感器 28 个委托） |
| `GlobalsRegistry` | Lua 全局变量注册表 |

### Aether.Platform.Workflow — 工作流引擎 (第二阶段)

**类 VisionMaster 的拖拽式可视化流程编排：**

| 组件 | 描述 |
|------|------|
| `WorkflowEngine` | 工作流执行引擎（节点调度、状态管理） |
| `WorkflowCanvas` | GDI+ 画布（节点渲染、连线、缩放平移） |
| `CanvasRenderer` | 画布渲染器 |
| `WorkflowDefinition` | 工作流定义（节点列表、连接线） |
| `WorkflowBuilder` | 工作流构建器（Fluent API） |
| `WorkflowNode` | 工作流节点基类 |
| `WorkflowNodeType` | 节点类型枚举 |
| `NodeConnection` | 节点连接线 |
| `NodeDragHandler` | 节点拖拽处理 |
| `NodeResult` | 节点执行结果 |
| `WorkflowState` | 工作流状态枚举 |
| `WorkflowExecutionHooks` | 执行钩子（日志、事件） |

**节点类型：**

| 分类 | 节点 |
|------|------|
| 流程控制 | 开始、结束、延时、条件分支、循环 |
| Lua 脚本 | Lua 脚本执行节点 |
| 轴控制 | 绝对运动、相对运动、回原点、停止 |
| IO 控制 | IO 置位/复位、读取、等待 |
| 扫码视觉 | 扫码、拍照、定位 |
| PLC 通信 | PLC 读、PLC 写、等待位 |
| MES 交互 | 数据上报、任务下载、信息查询 |

### Aether.Platform.UI — 界面层 (WinForms)

**Shell 架构：**

- **单窗口 Shell + 6 个 Tab 导航 + ContentPanel 动态加载**
- **缓存池模式** — 首次加载后 Hide/Show 切换，不重建控件树
- **MVP 模式** — View-Presenter 分离
- **1920×1080 主分辨率**，向下兼容 1280×1024（Anchor/Dock 自适应）

**模块视图：**

| 视图 | 功能 |
|------|------|
| `MainShellForm` | 主 Shell 窗体（三色灯、报警横幅、节拍显示、F1-F8 快捷键） |
| `NavigationManager` | 导航管理器 |
| `MainView` | 主界面（生产信息、曲线、NG 饼图、工站状态、料仓视觉、辅料） |
| `StatusLogView` | 状态日志（工站状态、运行/暂停/复位按钮） |
| `ControlDebugView` | 控制调试（IO/轴/工作点位/通信/模拟量/料仓调试） |
| `VisionDebugView` | 视觉调试（相机列表、视觉工具、参数导入导出） |
| `ProcessDebugView` | 工艺调试（件号参数：设备/工艺/信息化分类） |
| `SystemConfigView` | 系统参数（软件信息、硬件配置、机型配置） |
| `VersionInfoView` | 版本信息（IFMS 模块状态、版本校验） |
| `LoginView` | 登录叠加层（半透明遮罩 + 登录卡片） |
| `HistoryView` | 历史记录 |
| `ScriptEditorView` | Lua 脚本编辑器（语法高亮、断点、单步调试） |
| `WorkflowEditorView` | 工作流画布编辑器 |
| `SimDataEditorView` | 仿真数据编辑器 |
| `SimulationPanel` | 仿真监控面板（可折叠/拖拽） |
| `InfoRowPanel` | 信息行面板 |

**子组件 (SubPanels)：**

| 面板 | 功能 |
|------|------|
| `InfoRowPanel` | MainView 中的 6 个子 Panel 容器 |

---

## 硬件仿真 Mock 层

工控项目核心痛点：没有硬件无法联调。仿真层让软件团队不等待硬件到位。

### 仿真硬件能力

| 仿真硬件 | 可模拟行为 |
|---------|-----------|
| SimulatedAxis | 运动插值、限位触发、噪声、故障注入 |
| SimulatedPLC | 寄存器读写、自动更新规则、急停/安全门信号 |
| SimulatedCamera | 本地图片素材、可调定位分数、模拟噪声 |
| SimulatedScanner | 预置码队列、可调成功率、连续失败模拟 |
| SimulatedDigitalIO | 输入输出模拟 |
| SimulatedAnalogIO | 模拟量电压/电流模拟 |
| SimulatedSerialPort | 串口收发模拟 |
| SimulatedModbus | Modbus RTU/TCP 模拟 |
| SimulatedHeightGauge | 测高值模拟 |
| SimulatedTemperature | 温度值模拟 |
| SimulatedMicropressure | 微压值模拟 |
| SimulatedEPValve | 比例阀模拟 |
| SimulatedExposureMeter | 曝光值模拟 |
| SimulatedSpotAnalyzer | 光斑值模拟 |
| SimulatedScale | 称重值模拟 |

### HardwareRecorder

录制真实硬件数据 → 保存 → 后续 Replay 模式回放，用于复现产线问题和回归测试。

### 仿真监控面板

仿真模式下自动展示可折叠/拖拽的监控面板，提供手动注入数据、触发故障、切换素材等操作入口。

---

## 数据库三模式策略

```
                      模式一                          模式二                          模式三
                 SqlServerOnly                    AccessOnly                SqlServerWithAccessFallback

                ┌──────────────┐              ┌──────────────┐              ┌──────────────┐    ┌──────────────┐
                │  SQL Server  │              │    Access    │              │  SQL Server  │←→→│    Access    │
                │    (主)      │              │    (主)      │              │    (主)      │故障│    (备)      │
                └──────────────┘              └──────────────┘              └──────────────┘    └──────────────┘

                纯 SQL Server，                纯 Access，                   SQL Server 优先，
                无本地备用                      单机离线模式                    故障时自动切换 Access

                适用：网络稳定，                  适用：无网络，                  适用：有网络，
                     有 DBA 的产线                    单机设备                        需高可用的产线
```

### 核心表结构 (三模式通用)

| 表名 | 用途 |
|------|------|
| `Users` | 用户与权限 |
| `Parameters` | 设备参数（按件号管理，支持 JSON/DB/双写三种持久化） |
| `ParameterChangeLogs` | 参数变更履历（不可删除） |
| `ProductionRecords` | 生产记录 |
| `GoldenSamples` | 封样记录 |
| `CheckLists` | 点检记录 |
| `AlarmRecords` | 报警记录 |
| `FaultCodes` | 故障代码库 |
| `OperationLogs` | 按钮操作记录 |
| `PLCTags` | PLC 标签映射 |
| `HardwareConfig` | 硬件配置 |
| `RecipeData` | 工艺配方 |
| `MaintenancePlans` | 维保计划 |
| `DataUploadQueue` | IFMS 上传队列（离线缓冲） |
| `DeviceSnapshots` | 设备状态快照（断电恢复） |

### 参数持久化策略

| 模式 | 说明 |
|------|------|
| `JsonFile` | 仅加密写入本地 JSON 文件 |
| `Database` | 仅存入 DB 的 Parameters 表 |
| `JsonWithDbSync` | JSON 为主 + DB 同步副本 |

---

## 设备清单 (13 种)

| 设备代号 | 类型 | 控制模式 |
|---------|------|---------|
| `MTFTest` | 测试/调试设备 | PlcBased |
| `ZG13` | 光学设备 | PlcBased |
| `XL07` | 光学设备 | PlcBased |
| `PX09` | 光学设备 | DirectControl |
| `DJ10` | 光学设备 | PlcBased |
| `ZZ14` | 组装设备 | PlcBased |
| `BB08` | 光学设备 | PlcBased |
| `DJ16` | 光学设备 | PlcBased |
| `SF16` | 光学设备 | PlcBased |
| `DB06` | 点胶设备 | PlcBased |
| `CC03` | 清洁设备 | DirectControl |
| `Assembly` | 组装设备 | PlcBased |
| `AssemblyNew` | 新组装设备 | PlcBased |

---

## 22 项业务功能

| # | 功能 | 关键描述 |
|---|------|---------|
| 1 | **登录权限** | 密码/刷卡/指纹(预留)、五级权限、IFMS 校验 |
| 2 | **IFMS 在线校验** | 线体/MAC/版本/件号/验收/人员/点检/订单 8 项 |
| 3 | **参数变更管理** | 变更履历不可删除、加密读写、一键导入导出 |
| 4 | **视觉调试** | 隐藏无需修改参数、简化交互 |
| 5 | **UI 界面** | 微软雅黑/Times New Roman 规范字体、模块化布局 |
| 6 | **封样** | 20+ 封样记录、自动封样、平均值计算 |
| 7 | **点检** | 开机/换件号强制点检、时间频率可设、批次更换强制 |
| 8 | **单步动作** | 模块组合动作、调试件号仅单步 |
| 9 | **料仓料盘** | 6 状态机（空→待确认→确认→待作业→作业中→完成） |
| 10 | **扫码** | 自动扫码、连续重码防呆、编码规则校验 |
| 11 | **码检测** | 码等级明细、位置校验、结果保存 |
| 12 | **追溯** | 前后道数量核对、产品工站校验 |
| 13 | **治具校验** | 治工具/辅料许可校验、寿命校验 |
| 14 | **NG 盘** | NG 与 OK 料隔离 |
| 15 | **产量统计** | 饼图、OEE、可修改（需动态密码） |
| 16 | **品质分析 SPC** | CPK/PPK/X-R 图表、箱形图、分等级预警停机 |
| 17 | **维保管理** | 自定义条目频率、轴/气动件作业次数追踪 |
| 18 | **故障代码** | DFMEA 故障提醒、原因+排查方法弹窗 |
| 19 | **断电数据保护** | 双写持久化、电池检测、上电防撞机、最后 3 颗数据校验 |
| 20 | **版本防呆** | 版本定期识别校验、后台更新记录 |
| 21 | **数据导出** | 按天自动导出、网络状态实时判断、断网本地存储 |
| 22 | **远程更新** | 服务器推送、静默安装、回滚机制、参数保留 |

---

## 基础服务架构

传统 G_ClassStatic 单体拆解为 11 个职责清晰的独立服务，通过 DI 容器管理：

```csharp
// 使用方式：通过 ServiceLocator 获取
var state = ServiceLocator.GetService<IStateService>();
state.SetStatus(MachineStatus.Running);
state.SetPartNumber("ABC123");

var param = ServiceLocator.GetService<IParameterService>();
var config = param.Load<DeviceConfig>("ABC123", "calibration");
param.Save("ABC123", "calibration", config);  // 自动记录变更履历

var alarm = ServiceLocator.GetService<IAlarmService>();
alarm.Raise(AlarmLevel.Error, "E001", "轴1驱动器故障", "检查驱动器电源和接线");
```

---

## 双引擎架构 (第二阶段)

### Lua 脚本引擎

基于 MoonSharp 2.15.0，支持完整 Lua 5.2 语法，28 个硬件绑定函数：

| Lua 函数 | C# 绑定 | 说明 |
|----------|---------|------|
| `axis_move_abs(id, pos)` | AxisMoveAbs | 绝对运动 |
| `axis_move_rel(id, delta)` | AxisMoveRel | 相对运动 |
| `axis_home(id)` | AxisHome | 回原点 |
| `axis_wait_done(id, timeout)` | AxisWaitStop | 等待轴停止 |
| `io_set(idx, val)` | DioWriteOutput | IO 置位 |
| `io_get(idx)` | DioReadInput | IO 读取 |
| `plc_read_d(addr)` | PlcReadD | PLC 读取 D 寄存器 |
| `plc_write_d(addr, val)` | PlcWriteD | PLC 写入 D 寄存器 |
| `scan_barcode(id, timeout)` | ScannerTrigger | 扫码 |
| `camera_capture(id)` | CameraCapture | 拍照 |
| `sensor_read(id)` | SensorRead | 传感器读数 |
| `print(msg)` | 内建 | 打印输出 |
| `msleep(ms)` | 内建 | 延时 |
| `log(level, msg)` | 内建 | 日志 |
| `assert(cond, msg)` | 内建 | 断言 |
| `now()` | 内建 | 当前时间 |

**Lua → 工作流分支映射：**

| Lua 返回值 | 工作流走向 |
|-----------|-----------|
| `return true` / `return 1` | → 完成 |
| `return false` / `return 0` | → 失败 |
| `return 2` | → 分支 A |
| `return 3` | → 分支 B |

### 工作流画布

- **节点工具箱** — 流程控制、Lua 脚本、轴控制、IO 控制、扫码视觉、PLC 通信、MES 交互
- **画布操作** — 拖拽创建、缩放平移、多选、撤销重做、对齐吸附
- **自动生成** — 从设备工步 (`DeviceStep.cs`) 自动构建工作流

---

## Shell UI 架构

```
MainShellForm
├── 三级状态灯 (🟢运行 / 🟡待机&调试 / 🔴报警&停机)
├── 报警横幅 (顶部滑入，5秒自动收起)
├── 连接状态条 (PLC/相机/IFMS 断连→红色闪烁)
├── 节拍显示 (当前 vs 目标，超时黄色高亮)
├── 键盘快捷键 (F1-F6 导航 Tab，F8 急停)
├── NavigationManager (Tab 可见性由 JSON 配置)
└── ContentPanel (缓存池模式加载模块视图)
    ├── MainView (6 个子 Panel + 独立 Presenter)
    ├── StatusLogView
    ├── ControlDebugView
    ├── VisionDebugView
    ├── ProcessDebugView
    └── SystemConfigView
```

### 字体规范

| 元素 | 字体 | 大小 | 样式 |
|------|------|------|------|
| 设备名 | 微软雅黑 | 25.8pt | Bold |
| 件号显示 | 微软雅黑 | 18pt | Bold, Underline |
| 点检状态 | 微软雅黑 | 18pt | Bold |
| 模块标题 | 微软雅黑 | 12pt | Bold |
| 模块内容 | 微软雅黑 | 12pt | — |
| 时间显示 | Times New Roman | 15pt | — |

---

## 多语言支持 (i18n)

| 语言 | 资源文件 | 文化代码 |
|------|---------|---------|
| 简体中文 | `Strings.resx` | zh-CN |
| 英语 | `Strings.en.resx` | en |
| 越南语 | `Strings.vi.resx` | vi-VN |

```csharp
var loc = ServiceLocator.GetService<ILocalizationService>();
lblTitle.Text = loc.T("Main_Title");  // → "主界面" / "Main" / "Màn hình chính"
```

---

## 自动流程控制

### 流程属性编号规范

| 编号范围 | 类别 | 说明 |
|---------|------|------|
| -3 | 关闭 | 系统关闭流程 |
| -2 | 件号初始化 | 件号切换时参数初始化 |
| -1 | 硬件初始化 | 上电硬件自检 |
| 1-99 | 界面操作 | 用户界面触发动作 |
| 100-199 | 调试 | 调试模式专用流程 |
| 200-299 | 复位 | 安全复位流程 |
| 300+ | 自动运行 | 全自动生产流程 |

### 后台线程模型 (8 线程 + 1 队列 + UI 编组)

| 线程 | 职责 | 周期 | 安全机制 |
|------|------|------|---------|
| UI 线程 (STA) | WinForms 控件 + 100ms 刷新 | 持续 | 唯一操作控件 |
| FlowRunner | 300+ 自动流程序列执行 | 持续 | `volatile` + `lock` |
| SafetyMonitor | 安全信号轮询 (100ms/50ms) | 高频定时 | 独立线程，不与 FlowRunner 共享锁 |
| HardwareStatePoller | PLC 寄存器 + 轴位置 → 内存缓存 | 200ms | `ReaderWriterLockSlim` |
| IfmsUploadWorker | BlockingCollection → HTTP 上行 | 持续消费 | BlockingCollection 内置安全 |
| IfmsHeartbeat | 设备在线心跳 | 60s | 只读 + HTTP |
| TimedTaskScheduler | 7 项定时任务调度 | 定时触发 | 独立 CTS |

### 定时后台任务

| 任务 | 周期 | 说明 |
|------|------|------|
| 图片清理 | 每天 02:00 | 超期图片删除 |
| 生产导出 | 每天 03:00 | 按天 CSV/Excel 导出 |
| 版本校验 | 启动 + 每 8h | 比对服务器版本 |
| 日志归档 | 每天 04:00 | 压缩旧日志 |
| IFMS 离线补传 | 每 30s | 检查队列自动上传 |
| 电池检测 | 启动 + 每 4h | UPS/主板电池预警 |

---

## 技术栈

| 组件 | 版本 | 用途 |
|------|------|------|
| .NET Framework | 4.7.2 | 开发框架 |
| C# | 7.3 | 编程语言 |
| WinForms | — | 桌面 UI |
| Microsoft.Extensions.DependencyInjection | 最新 | 依赖注入容器 |
| Dapper | 2.1.28 | 轻量 ORM |
| NLog | 5.2.0 | 日志框架 |
| Newtonsoft.Json | 13.0.3 | JSON 序列化 |
| MoonSharp | 2.15.0 | Lua 5.2 脚本引擎 (Phase 2) |
| AvalonEdit | 6.3.0 | 代码编辑器 (Phase 2) |
| EasyModbus | latest | Modbus TCP 通信 |
| SQL Server | 2016+ | 关系数据库 |
| Access | 2016+ (.accdb) | 本地数据库 |

---

## 环境要求与构建

### 开发环境

- **Visual Studio 2017 / 2019 / 2022**（推荐 2022）
- **.NET Framework 4.7.2 开发工具包**
- **SQL Server 2016+**（模式一/三使用，模式二不需要）
- **Access 2016+**（模式二/三使用，模式一不需要）

### 构建步骤

```bash
# 1. 克隆仓库
git clone <your-repo-url>
cd <repo>/src

# 2. 打开解决方案
start Aether.Platform.sln
# 或在 Visual Studio 中打开

# 3. 还原 NuGet 包
nuget restore Aether.Platform.sln
# 或在 VS 中：右键解决方案 → 还原 NuGet 包

# 4. 编译
msbuild Aether.Platform.sln /p:Configuration=Debug
# 或在 VS 中：生成 → 生成解决方案
```

### 必需 NuGet 包

```powershell
Install-Package NLog -Version 5.2.0
Install-Package Newtonsoft.Json -Version 13.0.3
Install-Package Dapper -Version 2.1.28
Install-Package System.Data.SqlClient -Version 4.8.6
Install-Package System.Data.OleDb
Install-Package Microsoft.Extensions.DependencyInjection
# Phase 2 可选
Install-Package MoonSharp -Version 2.15.0
Install-Package AvalonEdit -Version 6.3.0
# PLC 通信
Install-Package EasyModbus
```

---

## 快速开始

### 1. 打开解决方案

```bash
# 进入 src 目录，双击解决方案文件
start Aether.Platform.sln
```

### 2. 配置启动项目

在 Visual Studio 中，右键 `Aether.Platform.App` → 设为启动项目。

### 3. 选择运行模式

```csharp
// Program.cs — 修改 RuntimeMode
var bootstrap = new AppBootstrap
{
    Mode = RuntimeMode.Simulation,  // Simulation: 仿真模式 | Real: 真实硬件
};
```

- **Simulation 模式**：无需任何硬件即可运行，所有硬件行为由仿真层模拟
- **Real 模式**：需要连接真实 PLC、相机等硬件设备

### 4. 选择设备

```csharp
// Program.cs — 加载指定设备
bootstrap.LoadDevice("MTFTest");
// 可选项: "ZG13", "XL07", "PX09", "DJ10", "ZZ14", "BB08", "DJ16", "SF16", "DB06", "CC03", "AssemblyNew", "Assembly"
```

### 5. 配置数据库 (可选)

```json
{
  "DeviceId": "MTFTest",
  "DatabaseConfig": {
    "Mode": "SqlServerOnly",
    "SQLServer": {
      "ConnectionString": "Server=.;Database=AetherPlatform;Integrated Security=True;"
    }
  }
}
```

### 6. 按 F5 运行

启动后自动：
- 初始化日志系统 (NLog)
- 加载设备配置
- 创建 DatabaseProvider
- 运行硬件初始化自检
- 显示 Shell 主界面

---

## 项目演进路线

### 阶段一：平台基础 + 设备适配 ✅

| 阶段 | 内容 | 状态 |
|------|------|------|
| P0 | 基础设施（项目框架、NLog、DI、三模式 DB、参数持久化） | ✅ |
| P1 | HAL 接口定义（14 类硬件接口） | ✅ |
| P2 | HAL-PLC 实现（Keyence + 汇川） | ✅ |
| P3 | HAL-运动控制（雷赛/固高/正运动适配器） | ✅ |
| P4 | HAL-视觉/IO/外设 | ✅ |
| P5 | 基础服务层（11 个服务替代 G_ClassStatic） | ✅ |
| P6 | 业务模块（生产/封样/点检/SPC/维保/追溯/导出） | ✅ |
| P7 | 设备适配（13 种设备工厂） | ✅ |

### 阶段二：脚本引擎 + 工作流画布 🔄

| 阶段 | 内容 | 状态 |
|------|------|------|
| P8 | Lua 引擎（MoonSharp 集成、硬件绑定） | ✅ |
| P9 | Lua 编辑器（语法高亮、调试、脚本库） | ✅ |
| P10 | 工作流引擎（WorkflowEngine、节点基类） | ✅ |
| P11 | 流程画布（GDI+ 画布、拖拽节点、缩放平移） | ✅ |
| P12 | 可视化节点（硬件节点、Lua 节点、属性编辑器） | 🔄 |
| P13 | 调试上线（单步、断点、两引擎融合测试） | 🔄 |

---

## 相关文档

| 文档 | 说明 |
|------|------|
| [平台化软件开发方案](../平台化软件开发方案.md) | 完整技术方案 (v3.6) |
| [README-快速开始](../README-快速开始.md) | 快速入门指南 |
| [修复与开发计划](../修复与开发计划.md) | 项目修复与开发计划 |
| [流程引擎开发方案](../自动化平台-流程引擎开发方案.md) | Lua 脚本 + 工作流画布设计 |

---

> **Aether 设备软件平台** — 让工控软件开发更高效、更可靠、更可维护。
