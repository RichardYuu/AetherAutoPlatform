using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Interfaces.Services;
using Aether.Platform.Core.Models;
using Aether.Platform.Core.Utilities;
using Aether.Platform.Services;
using Aether.Platform.Business;
using Aether.Platform.Data.Database;
using Aether.Platform.Data.Configuration;
using Aether.Platform.Data.Ifms;
using Aether.Platform.Data.Repository;
using Aether.Platform.HAL;
using Aether.Platform.Devices;
using Aether.Platform.Scripting;
using Aether.Platform.Workflow;

namespace Aether.Platform.App
{
    /// <summary>硬件运行模式</summary>
    public enum RuntimeMode
    {
        Simulation,
        Real,
    }

    /// <summary>
    /// 启动引导器 —— 统一初始化所有子系统。
    /// 基于 Microsoft.Extensions.DependencyInjection 管理服务注册与生命周期。
    /// 所有服务统一通过 ServiceCollection 注册，由 IServiceProvider 统一解析。
    /// </summary>
    public class AppBootstrap
    {
        // ============================================================
        //  DI 容器
        // ============================================================

        /// <summary>MS DI 服务集合，构建后保留以支持运行时动态注册</summary>
        private ServiceCollection _services;

        /// <summary>MS DI 服务提供者</summary>
        private IServiceProvider _serviceProvider;

        // ============================================================
        //  配置
        // ============================================================

        public RuntimeMode Mode { get; set; } = RuntimeMode.Simulation;
        public string ActiveDeviceType { get; private set; }
        public IDevice ActiveDevice { get; private set; }
        public bool IsInitialized { get; private set; }

        // ============================================================
        //  服务属性（保持向后兼容，均从 DI 容器解析）
        // ============================================================

        // HAL
        public IHardwareService HardwareService => _serviceProvider?.GetService<IHardwareService>();

        // Services
        public StateService StateService => _serviceProvider?.GetService<StateService>();
        public ParameterService ParamService => _serviceProvider?.GetService<ParameterService>();
        public ConfigurationService ConfigService => _serviceProvider?.GetService<ConfigurationService>();
        public AlarmService AlarmService => _serviceProvider?.GetService<AlarmService>();
        public AuthService AuthService => _serviceProvider?.GetService<AuthService>();
        public AuditService AuditService => _serviceProvider?.GetService<AuditService>();
        public InitializationService InitService => _serviceProvider?.GetService<InitializationService>();
        public FlowPropertyService FlowPropService => _serviceProvider?.GetService<FlowPropertyService>();
        public FlowRunnerService FlowRunner => _serviceProvider?.GetService<FlowRunnerService>();
        public TimedTaskSchedulerService TaskScheduler => _serviceProvider?.GetService<TimedTaskSchedulerService>();
        public LocalizationService LocService => _serviceProvider?.GetService<LocalizationService>();

        // Business
        public ProductionService ProductionSvc => _serviceProvider?.GetService<ProductionService>();
        public GoldenSampleService GoldenSampleSvc => _serviceProvider?.GetService<GoldenSampleService>();
        public CheckListService CheckListSvc => _serviceProvider?.GetService<CheckListService>();
        public QualityService QualitySvc => _serviceProvider?.GetService<QualityService>();
        public TraceabilityService TraceabilitySvc => _serviceProvider?.GetService<TraceabilityService>();
        public MaintenanceService MaintenanceSvc => _serviceProvider?.GetService<MaintenanceService>();
        public ExportService ExportSvc => _serviceProvider?.GetService<ExportService>();

        // Data
        public DbContext DbContext => _serviceProvider?.GetService<DbContext>();
        public IfmsHttpClient IfmsClient => _serviceProvider?.GetService<IfmsHttpClient>();

        // Scripting
        public LuaScriptEngine LuaEngine => _serviceProvider?.GetService<LuaScriptEngine>();
        public HardwareBindings HWBindings => _serviceProvider?.GetService<HardwareBindings>();
        public GlobalsRegistry Globals => _serviceProvider?.GetService<GlobalsRegistry>();

        // Workflow
        public WorkflowEngine ActiveWorkflow { get; set; }
        public WorkflowDefinition CurrentWorkflowDefinition { get; private set; }

        /// <summary>
        /// 安全地在同步上下文中执行异步 Task，避免 SyncContext 死锁。
        /// MoonSharp Lua 引擎要求委托为同步签名，因此在 Lua→硬件绑定的桥接处使用。
        /// </summary>
        private static T RunSync<T>(Func<Task<T>> task) => task().GetAwaiter().GetResult();
        private static void RunSync(Func<Task> task) => task().GetAwaiter().GetResult();

        // 日志回调
        public Action<string, string> OnSystemLog;

        // ---- 单例 ----
        private static AppBootstrap _instance;
        public static AppBootstrap Instance => _instance;

        public AppBootstrap()
        {
            _instance = this;
        }

        // ============================================================
        //  初始化流程
        // ============================================================

        /// <summary>
        /// 完整初始化流程。
        /// 1. 创建 ServiceCollection 并注册所有服务
        /// 2. 构建 IServiceProvider（启用校验）
        /// 3. 注入到 ServiceLocator 供全局访问
        /// 4. 启动后台任务
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            try
            {
                Log("System", "===== 平台初始化开始 =====");

                // 1. 创建 DI 容器并注册所有服务
                _services = new ServiceCollection();
                RegisterAllServices();

                // 2. 构建容器（validateOnBuild = true 可在构建时发现循环依赖和缺失注册）
                _serviceProvider = _services.BuildServiceProvider();

                // 3. 注入到全局 ServiceLocator，并保留 ServiceCollection 以支持运行时动态注册
                ServiceLocator.SetProvider(_serviceProvider, _services);

                // 4. 注册硬件绑定到 Lua 引擎
                RegisterHardwareBindings();

                // 5. 启动后台定时任务
                StartBackgroundTasks();

                IsInitialized = true;
                Log("System", "===== 平台初始化完成 =====");
            }
            catch (Exception ex)
            {
                Log("System", $"初始化失败: {ex.Message}");
                MessageBox.Show($"平台初始化失败:\n{ex.Message}\n\n请检查配置后重试。",
                    "启动错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 注册所有服务到 DI 容器。
        /// 分为三个层次：数据层 → 核心服务层 → 业务层 + 脚本引擎。
        /// </summary>
        private void RegisterAllServices()
        {
            // ========================
            // 1. 数据层（数据库 + 仓储 + IFMS）
            // ========================
            Log("System", "[1/5] 注册数据层...");

            var config = ConfigManager.Load();
            var provider = DatabaseProviderFactory.Create(
                config.DatabaseMode,
                config.SqlServerConnectionString,
                config.AccessConnectionString);

            // 数据库上下文 —— 单例，ParameterService 通过 DI 获取此实例
            _services.AddSingleton(new DbContext(provider));

            // 仓储层 —— 持久化业务数据到本地 JSON 文件（Data/ 目录），重启不丢失
            _services.AddSingleton<IProductionRecordRepository>(new ProductionRecordRepository());
            _services.AddSingleton<IQualityRecordRepository>(new QualityRecordRepository());
            _services.AddSingleton<ITraceabilityRecordRepository>(new TraceabilityRecordRepository());
            _services.AddSingleton<IMaintenanceRecordRepository>(new MaintenanceRecordRepository());

            Log("System", "  ✓ 4 个仓储已注册（本地 JSON 持久化）");

            // IFMS 通讯客户端 —— 单例；接口 IIfmsBroker 由 AllServices 层动态注册
            _services.AddSingleton(new IfmsHttpClient());

            Log("System", "  ✓ 数据层就绪");

            // ========================
            // 2. 核心服务（11 个无状态/轻状态服务，全部单例）
            // ParameterService 通过 MS DI 自动注入 DbContext，不再自行创建 IDatabaseProvider
            // ========================
            Log("System", "[2/5] 注册核心服务...");

            _services.AddSingleton<StateService>();
            _services.AddSingleton<ParameterService>();
            _services.AddSingleton<ConfigurationService>();
            _services.AddSingleton<AlarmService>();
            _services.AddSingleton<AuthService>();
            _services.AddSingleton<AuditService>();
            _services.AddSingleton<InitializationService>();
            _services.AddSingleton<FlowPropertyService>();
            _services.AddSingleton<FlowRunnerService>();
            _services.AddSingleton<TimedTaskSchedulerService>();
            _services.AddSingleton<LocalizationService>();

            Log("System", "  ✓ 11 个核心服务已注册");

            // ========================
            // 3. 硬件抽象层（HAL）
            // ========================
            Log("System", $"[3/5] 注册硬件抽象层 (模式: {Mode})...");

            var halMode = Mode == RuntimeMode.Simulation
                ? HardwareServiceMode.Simulated
                : HardwareServiceMode.Real;

            var hwService = HardwareServiceFactory.Create(halMode);
            if (hwService == null)
            {
                Log("System", $"  警告: HAL 工厂未返回 {halMode} 模式，回退到 Simulation");
                hwService = HardwareServiceFactory.Create(HardwareServiceMode.Simulated);
            }
            _services.AddSingleton<IHardwareService>(hwService);

            Log("System", $"  ✓ HAL 就绪 ({halMode})");

            // ========================
            // 4. 业务层（7 个业务服务，全部单例）
            // 各服务通过构造函数注入仓储接口，自动加载/保存持久化数据
            // ========================
            Log("System", "[4/5] 注册业务层...");

            _services.AddSingleton<ProductionService>();
            _services.AddSingleton<GoldenSampleService>();
            _services.AddSingleton<CheckListService>();
            _services.AddSingleton<QualityService>();
            _services.AddSingleton<TraceabilityService>();
            _services.AddSingleton<MaintenanceService>();
            _services.AddSingleton<ExportService>();

            Log("System", "  ✓ 7 个业务服务已注册");

            // ========================
            // 5. 脚本引擎
            // ========================
            Log("System", "[5/5] 注册脚本引擎...");

            var globals = new GlobalsRegistry();
            var hwBindings = new HardwareBindings();
            _services.AddSingleton(globals);
            _services.AddSingleton(hwBindings);
            _services.AddSingleton(new LuaScriptEngine(globals));

            Log("System", "  ✓ Lua 脚本引擎已注册");
        }

        private void StartBackgroundTasks()
        {
            Log("System", "启动后台任务...");

            var taskScheduler = TaskScheduler;
            var stateService = StateService;

            // 定时心跳
            taskScheduler?.Register("Heartbeat", TimeSpan.FromSeconds(5), async ct =>
            {
                stateService?.SetStatus(MachineStatus.Running);
                await Task.CompletedTask;
            });

            // 数据保存（每30秒）
            taskScheduler?.Register("AutoSave", TimeSpan.FromSeconds(30), async ct =>
            {
                ConfigManager.Save();
                await Task.CompletedTask;
            });

            Log("System", "  ✓ 后台定时任务已启动");
        }

        private void RegisterHardwareBindings()
        {
            var hwService = HardwareService;
            if (hwService == null) return;

            const double SpeedSlow = 10;
            const double SpeedNormal = 50;
            const int ScanTimeoutMs = 3000;
            var ct = CancellationToken.None;

            var hwBindings = HWBindings;

            // 轴操作
            hwBindings.AxisGetPos = (axisId) =>
            {
                var axis = hwService.GetAxis(axisId.ToString());
                return (int)(axis.CurrentPosition * 1000);
            };
            hwBindings.AxisHome = (axisId) =>
            {
                RunSync(() => hwService.GetAxis(axisId.ToString()).HomeAsync(ct));
                return true;
            };
            hwBindings.AxisMoveAbs = (axisId, pos) =>
            {
                RunSync(() => hwService.GetAxis(axisId.ToString()).MoveAbsAsync(pos / 1000.0, SpeedNormal, ct));
                return true;
            };
            hwBindings.AxisMoveRel = (axisId, delta) =>
            {
                RunSync(() => hwService.GetAxis(axisId.ToString()).MoveRelAsync(delta / 1000.0, SpeedSlow, ct));
                return true;
            };
            hwBindings.AxisStop = (axisId) =>
            {
                RunSync(() => hwService.GetAxis(axisId.ToString()).StopAsync(ct));
                return true;
            };
            hwBindings.AxisWaitStop = (axisId, timeoutMs) =>
            {
                RunSync(() => Task.Delay(timeoutMs));
                return true;
            };

            // DIO
            hwBindings.DioReadInput = (idx) =>
            {
                var dio = hwService.GetDigitalIO(idx.ToString());
                return dio != null ? RunSync(() => dio.ReadAsync(ct)) : false;
            };
            hwBindings.DioReadOutput = (idx) =>
            {
                var dio = hwService.GetDigitalIO(idx.ToString());
                return dio != null ? RunSync(() => dio.ReadAsync(ct)) : false;
            };
            hwBindings.DioWriteOutput = (idx, val) =>
            {
                RunSync(() => hwService.GetDigitalIO(idx.ToString())?.WriteAsync(val, ct) ?? Task.CompletedTask);
                return true;
            };
            hwBindings.DioToggleOutput = (idx) =>
            {
                var dio = hwService.GetDigitalIO(idx.ToString());
                if (dio == null) return false;
                var current = RunSync(() => dio.ReadAsync(ct));
                RunSync(() => dio.WriteAsync(!current, ct));
                return true;
            };

            // PLC
            var plc = hwService.GetPlc();
            hwBindings.PlcReadD = (addr) => plc != null ? RunSync(() => plc.ReadWordAsync(addr, ct)) : 0;
            hwBindings.PlcWriteD = (addr, val) => { if (plc != null) RunSync(() => plc.WriteWordAsync(addr, val, ct)); };
            hwBindings.PlcReadM = (addr) => plc != null ? RunSync(() => plc.ReadBitAsync(addr, ct)) : false;
            hwBindings.PlcWriteM = (addr, val) => { if (plc != null) RunSync(() => plc.WriteBitAsync(addr, val, ct)); };

            // 相机/扫码器
            hwBindings.ScannerTrigger = () =>
            {
                var scanner = hwService.GetScanner("default");
                return scanner != null ? RunSync(() => scanner.ScanAsync(ScanTimeoutMs, ct)) : "";
            };
            hwBindings.CameraCapture = (camId) =>
            {
                var vision = hwService.GetVisionSystem(camId);
                var result = vision != null ? RunSync(() => vision.CaptureAsync(ct)) : null;
                return result != null ? Convert.ToBase64String(result) : "";
            };

            // 传感器
            hwBindings.SensorRead = (sensorId) =>
            {
                var analog = hwService.GetAnalogIO(sensorId);
                return analog != null ? RunSync(() => analog.ReadValueAsync(ct)) : 0.0;
            };

            // 注入硬件绑定到 Lua 引擎
            var luaEngine = LuaEngine;
            luaEngine?.SetHardwareBindings(hwBindings);

            // 设为全局实例，供 ScriptEditorView 等 UI 层访问
            HardwareBindings.Current = hwBindings;

            Log("System", "  ✓ 硬件函数已绑定到 Lua 引擎 (28 个委托)");
        }

        // ============================================================
        //  设备管理
        // ============================================================

        /// <summary>加载指定设备类型</summary>
        public IDevice LoadDevice(string deviceType)
        {
            ActiveDeviceType = deviceType;
            ActiveDevice = DeviceFactory.Create(deviceType);

            if (ActiveDevice != null)
            {
                ActiveDevice.Initialize();
                Log("System", $"加载设备: {ActiveDevice.DeviceName} (v{ActiveDevice.Version})");

                // 自动生成工作流
                GenerateWorkflowForDevice();
            }
            else
            {
                Log("System", $"警告: 未找到设备类型 '{deviceType}'");
            }

            return ActiveDevice;
        }

        private void GenerateWorkflowForDevice()
        {
            if (ActiveDevice == null) return;

            var dev = ActiveDevice.GetType();
            string name = dev.Name.Replace("Device", "");

            var builder = new WorkflowBuilder(
                id: $"WF_{name}",
                name: $"{ActiveDevice.DeviceName} 工作流",
                deviceType: ActiveDevice.DeviceType
            );

            // 从设备工步构建工作流
            builder.Start($"{ActiveDevice.DeviceName} 开始");

            if (ActiveDevice is Devices.Base.DeviceBase devBase)
            {
                foreach (var step in devBase.Steps)
                {
                    switch (step.ActionName)
                    {
                        case var a when a.StartsWith("SCAN"):
                            builder.ScannerRead();
                            break;
                        case var a when a.Contains("HOME"):
                            builder.AxisMove("Z", 0, 30)
                                   .Delay(2000);
                            break;
                        case var a when a.Contains("DISPENSE"):
                            builder.LuaScript($"print('{step.Description}')\nmsleep(3000)", step.Description);
                            break;
                        case var a when a.Contains("UV") || a.Contains("CURE"):
                            builder.Delay(3000);
                            break;
                        case var a when a.Contains("CAPTURE") || a.Contains("VISION"):
                            builder.VisionCapture("default");
                            break;
                        case var a when a.Contains("PRESS") || a.Contains("压"):
                            builder.AxisMove("U", -50, 20)
                                   .Delay(2000);
                            break;
                        case var a when a.Contains("Dio") || a.Contains("IO") || a.Contains("LIGHT"):
                            builder.DioWrite(0, true)
                                   .Delay(500);
                            break;
                        case var a when a.Contains("UPLOAD"):
                            builder.LuaScript($"log('INFO', '上传 {step.Description}')", step.Description);
                            break;
                        case var a when a.Contains("WAIT") || a.Contains("等待"):
                            builder.WaitFor(step.ActionName, "OK");
                            break;
                        default:
                            builder.Delay((int)step.EstimatedDuration.TotalMilliseconds);
                            break;
                    }
                }
            }

            builder.End($"{ActiveDevice.DeviceName} 结束");
            CurrentWorkflowDefinition = builder.Build();

            // 创建执行器
            var hooks = new WorkflowExecutionHooks
            {
                LogCallback = (category, msg) => Log(category, msg),
            };
            ActiveWorkflow = new WorkflowEngine(CurrentWorkflowDefinition, hooks);

            Log("System", $"  ✓ 工作流 '{CurrentWorkflowDefinition.Name}' 已生成 ({CurrentWorkflowDefinition.Nodes.Count} 节点)");
        }

        /// <summary>启动当前设备工作流</summary>
        public async Task StartDeviceWorkflowAsync()
        {
            if (ActiveWorkflow == null)
            {
                Log("System", "没有活跃的工作流");
                return;
            }
            ActiveDevice?.Start();
            await ActiveWorkflow.RunAsync();
        }

        /// <summary>停止当前设备工作流</summary>
        public void StopDeviceWorkflow()
        {
            ActiveWorkflow?.Abort();
            ActiveDevice?.Stop();
        }

        // ============================================================
        //  日志
        // ============================================================

        private void Log(string category, string message)
        {
            OnSystemLog?.Invoke(category, $"[{DateTime.Now:HH:mm:ss}] {message}");
            System.Diagnostics.Debug.WriteLine($"[{category}] {message}");
        }
    }
}