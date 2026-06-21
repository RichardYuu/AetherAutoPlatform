# Aether 设备软件平台 API 调用文档

> **文档版本**: v3.6  
> **最后更新**: 2026-06-21  
> **适用对象**: 开发者、集成商、二次开发人员

---

## 目录

1. [服务定位器 API](#1-服务定位器-api)
2. [核心服务 API](#2-核心服务-api)
3. [硬件抽象层 (HAL) API](#3-硬件抽象层-hal-api)
4. [业务服务 API](#4-业务服务-api)
5. [数据访问 API](#5-数据访问-api)
6. [IFMS 通信 API](#6-ifms-通信-api)
7. [设备适配 API](#7-设备适配-api)
8. [脚本引擎 API (Lua)](#8-脚本引擎-api-lua)
9. [工作流引擎 API](#9-工作流引擎-api)
10. [数据模型](#10-数据模型)
11. [枚举类型](#11-枚举类型)

---

## 1. 服务定位器 API

### ServiceLocator

全局服务定位器，基于 Microsoft.Extensions.DependencyInjection 封装。

#### 静态方法

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `GetService<T>()` | 获取已注册的服务实例 | 无 | `T` 服务实例 |
| `TryGetService<T>(out T)` | 尝试获取服务，失败时返回 false | `out T service` | `bool` |
| `RegisterSingleton<T>(instance)` | 运行时动态注册单例服务 | `T instance` | 无 |
| `RegisterLazy<T>(factory)` | 运行时动态注册延迟实例化服务 | `Func<object> factory` | 无 |
| `SetProvider(provider, collection)` | 初始化容器（AppBootstrap 调用） | `IServiceProvider provider`, `ServiceCollection collection` | 无 |
| `Reset()` | 重置容器（仅测试用） | 无 | 无 |

#### 使用示例

```csharp
// 获取服务
var state = ServiceLocator.GetService<IStateService>();
state.SetStatus(MachineStatus.Running);

// 尝试获取（不抛异常）
if (ServiceLocator.TryGetService<IIfmsBroker>(out var ifms))
{
    ifms.UploadProductionDataAsync(data);
}

// 动态注册（延迟初始化）
ServiceLocator.RegisterSingleton<IIfmsBroker>(new IfmsHttpClient());
```

---

## 2. 核心服务 API

### 2.1 IStateService - 状态服务

管理运行时状态（机台状态、权限、件号、点检等）。

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `MachineStatus` (get) | 当前机台状态 | 无 | `MachineStatus` |
| `CurrentPartNumber` (get) | 当前件号 | 无 | `string` |
| `CurrentUser` (get) | 当前用户 ID | 无 | `string` |
| `CurrentRole` (get) | 当前用户角色 | 无 | `UserRole` |
| `Get<T>(key)` | 从状态字典获取值 | `string key` | `T` |
| `Set(key, value)` | 设置状态字典值 | `string key, object value` | 无 |
| `SetStatus(status)` | 设置机台状态 | `MachineStatus status` | 无 |
| `SetPartNumber(partNumber)` | 设置当前件号 | `string partNumber` | 无 |
| `TakeSnapshot(reason)` | 拍摄状态快照（断电恢复） | `string reason` | 无 |
| `RestoreSnapshot()` | 恢复最近一次快照 | 无 | 无 |

**事件**:
- `OnStatusChanged(MachineStatus)` - 机台状态变化
- `OnPartNumberChanged(string)` - 件号变化

#### 使用示例

```csharp
var state = ServiceLocator.GetService<IStateService>();

// 设置状态
state.SetStatus(MachineStatus.Running);
state.SetPartNumber("ABC123");

// 存储运行时数据
state.Set("CurrentAxisPosition", new Dictionary<string, double> { ["X"] = 100.0 });

// 断电快照
state.TakeSnapshot("power-off-protection");

// 恢复快照
state.RestoreSnapshot();

// 监听状态变化
state.OnStatusChanged += status => {
    Console.WriteLine($"状态变为: {status}");
};
```

---

### 2.2 IParameterService - 参数服务

设备/工艺参数的 CRUD 操作，支持三种持久化模式。

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `PersistenceMode` (get) | 参数持久化模式 | 无 | `ParameterPersistenceMode` |
| `Load<T>(partNumber, paramName)` | 加载参数 | `string partNumber, string paramName` | `T` |
| `Save<T>(partNumber, paramName, data)` | 保存参数 | `string partNumber, string paramName, T data` | 无 |
| `ExportAll(partNumber, filePath)` | 导出所有参数到文件 | `string partNumber, string filePath` | 无 |
| `ImportAll(partNumber, filePath)` | 从文件导入所有参数 | `string partNumber, string filePath` | 无 |
| `GetChangeLogs(partNumber, paramName)` | 获取变更履历 | `string partNumber, string paramName` | `IReadOnlyList<ParameterChangeLog>` |

#### 持久化模式

| 模式 | 说明 |
|------|------|
| `JsonFile` | 加密写入本地 JSON 文件 |
| `Database` | 存入 DB Parameters 表 |
| `JsonWithDbSync` | JSON 为主 + DB 同步副本 |

#### 使用示例

```csharp
var param = ServiceLocator.GetService<IParameterService>();

// 定义参数类
public class DeviceConfig
{
    public double ExposureTime { get; set; }
    public int VisionThreshold { get; set; }
    public string CameraRecipe { get; set; }
}

// 保存参数
var config = new DeviceConfig { ExposureTime = 10.5, VisionThreshold = 85 };
param.Save("ABC123", "calibration", config);

// 加载参数
var loaded = param.Load<DeviceConfig>("ABC123", "calibration");
Console.WriteLine($"曝光时间: {loaded.ExposureTime}");

// 获取变更履历
var logs = param.GetChangeLogs("ABC123", "calibration");
foreach (var log in logs)
{
    Console.WriteLine($"{log.ChangedBy}: {log.OldValue} → {log.NewValue}");
}

// 导出/导入
param.ExportAll("ABC123", "D:\\backup\\ABC123_params.json");
param.ImportAll("ABC123", "D:\\backup\\ABC123_params.json");
```

---

### 2.3 IConfigurationService - 配置服务

应用/硬件配置的加载与热重载。

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `GetSection<T>(sectionName)` | 获取配置节 | `string sectionName` | `T` |
| `GetValue(key)` | 获取配置值 | `string key` | `string` |
| `SetValue(key, value)` | 设置配置值 | `string key, string value` | 无 |
| `Reload()` | 重新加载配置文件 | 无 | 无 |

**事件**:
- `OnConfigurationChanged` - 配置变化

#### 使用示例

```csharp
var config = ServiceLocator.GetService<IConfigurationService>();

// 获取配置节
var dbConfig = config.GetSection<DatabaseConfig>("Database");
Console.WriteLine($"连接串: {dbConfig.ConnectionString}");

// 热重载
config.SetValue("Vision.Timeout", "5000");
config.Reload();
```

---

### 2.4 IAlarmService - 报警服务

三级报警管理（Error/Tip/Trace）。

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `Raise(level, code, message, suggestion)` | 触发报警 | `AlarmLevel level, string code, string message, string suggestion` | 无 |
| `Clear(code)` | 清除报警 | `string code` | 无 |
| `GetActiveAlarms()` | 获取活跃报警 | 无 | `IReadOnlyList<AlarmRecord>` |
| `GetHistory(from, to)` | 获取历史报警 | `DateTime from, DateTime to` | `IReadOnlyList<AlarmRecord>` |

**事件**:
- `OnAlarmRaised(AlarmRecord)` - 报警触发
- `OnAlarmCleared(AlarmRecord)` - 报警清除

#### 使用示例

```csharp
var alarm = ServiceLocator.GetService<IAlarmService>();

// 触发报警
alarm.Raise(AlarmLevel.Error, "E001", "轴1驱动器故障", "检查驱动器电源和接线");
alarm.Raise(AlarmLevel.Tip, "T001", "维保即将到期，剩余12小时");

// 清除报警
alarm.Clear("E001");

// 获取活跃报警
var active = alarm.GetActiveAlarms();
foreach (var a in active)
{
    Console.WriteLine($"{a.Code}: {a.Message}");
}

// 监听报警
alarm.OnAlarmRaised += record => {
    MessageBox.Show($"报警: {record.Code} - {record.Message}");
};
```

---

### 2.5 IAuthService - 认证服务

多方式登录认证，五级权限管理。

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `LoginAsync(userId, password)` | 账号密码登录 | `string userId, string password` | `Task<LoginResult>` |
| `LoginWithCardAsync(cardId)` | 刷卡登录 | `string cardId` | `Task<LoginResult>` |
| `LoginWithFingerprintAsync(data)` | 指纹登录（预留） | `byte[] data` | `Task<LoginResult>` |
| `LoginWithFaceAsync(data)` | 人脸识别（预留） | `byte[] data` | `Task<LoginResult>` |
| `LoginWithUSBKeyAsync()` | U 盘密码狗登录 | 无 | `Task<LoginResult>` |
| `ValidateDynamicPassword(password)` | 验证动态密码 | `string password` | `bool` |
| `ValidateIFMSAccess(userId, deviceId)` | 验证 IFMS 访问 | `string userId, string deviceId` | `bool` |
| `IsUSBKeyExpired` (get) | U 盘狗是否过期 | 无 | `bool` |
| `ActivateUSBKey(code)` | 激活 U 盘狗 | `string code` | `bool` |
| `Logout()` | 登出 | 无 | 无 |

#### 使用示例

```csharp
var auth = ServiceLocator.GetService<IAuthService>();

// 账号密码登录
var result = await auth.LoginAsync("user01", "password123");
if (result.Success)
{
    Console.WriteLine($"欢迎, {result.UserName}! 角色: {result.Role}");
}

// 刷卡登录
var cardResult = await auth.LoginWithCardAsync("CARD_123456");

// 验证动态密码（敏感操作）
if (auth.ValidateDynamicPassword("8888"))
{
    // 允许执行敏感操作
    productionService.ResetCount(userId, dynamicPassword);
}

// 登出
auth.Logout();
```

---

### 2.6 IFlowRunner - 流程执行服务

控制自动流程线程（启动/暂停/停止/复位/急停）。

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `CurrentState` (get) | 当前流程状态 | 无 | `FlowState` |
| `CurrentStepCode` (get) | 当前流程步骤编号 | 无 | `int` |
| `StartAsync(startCode, ct)` | 启动流程 | `int startCode, CancellationToken ct` | `Task` |
| `PauseAsync()` | 暂停流程 | 无 | `Task` |
| `ResumeAsync()` | 恢复流程 | 无 | `Task` |
| `StopAsync()` | 安全停止 | 无 | `Task` |
| `EmergencyStopAsync()` | 急停（立即停止） | 无 | `Task` |
| `ResetAsync()` | 复位（回 Idle） | 无 | `Task` |

**事件**:
- `OnStateChanged(FlowState)` - 流程状态变化
- `OnStepChanged(int, string)` - 流程步骤变化

#### 使用示例

```csharp
var flowRunner = ServiceLocator.GetService<IFlowRunner>();

// 启动自动流程（300+ 为自动运行）
await flowRunner.StartAsync(300, CancellationToken.None);

// 暂停
await flowRunner.PauseAsync();

// 恢复
await flowRunner.ResumeAsync();

// 急停
await flowRunner.EmergencyStopAsync();

// 复位
await flowRunner.ResetAsync();

// 监听状态变化
flowRunner.OnStateChanged += state => {
    Console.WriteLine($"流程状态: {state}");
};

flowRunner.OnStepChanged += (code, name) => {
    Console.WriteLine($"当前步骤: {code} - {name}");
};
```

---

### 2.7 ITimedTaskScheduler - 定时任务调度

管理 7 项定时后台任务。

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `Register(taskName, interval, action)` | 注册定时任务 | `string taskName, TimeSpan interval, Func<CancellationToken, Task> action` | 无 |
| `Unregister(taskName)` | 取消定时任务 | `string taskName` | 无 |
| `GetStatus()` | 获取任务状态 | 无 | `IReadOnlyList<TimedTaskStatus>` |

#### 使用示例

```csharp
var scheduler = ServiceLocator.GetService<ITimedTaskScheduler>();

// 注册定时任务（每 30 秒）
scheduler.Register("IFMS 离线补传", TimeSpan.FromSeconds(30), async (ct) => {
    var ifms = ServiceLocator.GetService<IIfmsBroker>();
    await ifms.FlushQueueAsync();
});

// 取消任务
scheduler.Unregister("IFMS 离线补传");

// 查看任务状态
var status = scheduler.GetStatus();
foreach (var s in status)
{
    Console.WriteLine($"{s.TaskName}: 上次运行 {s.LastRunTime}");
}
```

---

### 2.8 ILocalizationService - 多语言服务

支持 zh-CN / en / vi-VN 三语切换。

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `CurrentLanguage` (get) | 当前语言 | 无 | `string` |
| `SupportedLanguages` (get) | 支持的语言列表 | 无 | `string[]` |
| `SwitchTo(cultureName)` | 切换语言 | `string cultureName` | 无 |
| `T(key)` | 获取翻译 | `string key` | `string` |
| `T(key, args)` | 获取带参数的翻译 | `string key, params object[] args` | `string` |

#### 使用示例

```csharp
var loc = ServiceLocator.GetService<ILocalizationService>();

// 获取翻译
lblTitle.Text = loc.T("Main_Title");  // → "主界面" / "Main" / "Màn hình chính"
lblCount.Text = loc.T("Production_Count", 150);  // 带参数

// 切换语言
loc.SwitchTo("en");

// 监听语言变化
loc.OnLanguageChanged += culture => {
    Console.WriteLine($"语言已切换为: {culture}");
};
```

---

## 3. 硬件抽象层 (HAL) API

### 3.1 IHardwareService - 统一硬件服务接口

获取所有硬件设备的访问接口。

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `GetAxis(axisId)` | 获取轴控制器 | `string axisId` | `IAxis` |
| `GetDigitalIO(ioId)` | 获取数字 IO | `string ioId` | `IDigitalIO` |
| `GetScanner(scannerId)` | 获取扫码器 | `string scannerId` | `IBarcodeScanner` |
| `GetVisionSystem(visionId)` | 获取视觉系统 | `string visionId` | `IVisionSystem` |
| `GetPlc()` | 获取 PLC 通信 | 无 | `IPlcCommunication` |
| `GetSerialPort(portName)` | 获取串口 | `string portName` | `ISerialPort` |
| `GetModbus(type, connectionString)` | 获取 Modbus | `ModbusType type, string connectionString` | `IModbusCommunication` |
| `GetAnalogIO(channel)` | 获取模拟量 IO | `int channel` | `IAnalogIO` |
| `GetScale(scaleId)` | 获取称重设备 | `string scaleId` | `IScaleDevice` |
| `GetHeightGauge(gaugeId)` | 获取测高仪 | `string gaugeId` | `IHeightGauge` |
| `GetTemperature(tempId)` | 获取温控器 | `string tempId` | `ITemperatureController` |
| `GetMicropressure(sensorId)` | 获取微压计 | `string sensorId` | `IMicropressureSensor` |
| `GetEPValve(valveId)` | 获取电气比例阀 | `string valveId` | `IElectricProportionalValve` |
| `GetExposureMeter(meterId)` | 获取曝光计 | `string meterId` | `IExposureMeter` |
| `GetSpotAnalyzer(analyzerId)` | 获取光斑仪 | `string analyzerId` | `ISpotAnalyzer` |
| `HasPlc` (get) | 是否使用 PLC 控制 | 无 | `bool` |

---

### 3.2 IAxis - 轴控制器

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `AxisId` (get) | 轴标识 | 无 | `string` |
| `IsEnabled` (get) | 是否已启用 | 无 | `bool` |
| `IsMoving` (get) | 是否正在运动 | 无 | `bool` |
| `CurrentPosition` (get) | 当前位置 | 无 | `double` |
| `IsHomed` (get) | 是否已回原点 | 无 | `bool` |
| `MoveAbsAsync(position, speed, ct)` | 绝对运动 | `double position, double speed, CancellationToken ct` | `Task` |
| `MoveRelAsync(distance, speed, ct)` | 相对运动 | `double distance, double speed, CancellationToken ct` | `Task` |
| `HomeAsync(ct)` | 回原点 | `CancellationToken ct` | `Task` |
| `StopAsync(ct)` | 停止 | `CancellationToken ct` | `Task` |
| `EnableAsync(ct)` | 启用 | `CancellationToken ct` | `Task` |
| `DisableAsync(ct)` | 禁用 | `CancellationToken ct` | `Task` |

#### 使用示例

```csharp
var hardware = ServiceLocator.GetService<IHardwareService>();
var axis = hardware.GetAxis("X");

// 绝对运动
await axis.MoveAbsAsync(100.0, 50.0, CancellationToken.None);

// 相对运动
await axis.MoveRelAsync(10.0, 30.0, CancellationToken.None);

// 回原点
await axis.HomeAsync(CancellationToken.None);

// 等待轴停止
while (axis.IsMoving)
{
    await Task.Delay(50);
}

// 检查位置
double pos = axis.CurrentPosition;
```

---

### 3.3 IDigitalIO - 数字 IO

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `IOId` (get) | IO 标识 | 无 | `string` |
| `IsInput` (get) | 是否输入点 | 无 | `bool` |
| `ReadAsync(ct)` | 读取状态 | `CancellationToken ct` | `Task<bool>` |
| `WriteAsync(value, ct)` | 写入状态 | `bool value, CancellationToken ct` | `Task` |

#### 使用示例

```csharp
var hardware = ServiceLocator.GetService<IHardwareService>();

// 输出点
var dioOut = hardware.GetDigitalIO("DO_LAMP");
await dioOut.WriteAsync(true, CancellationToken.None);  // 开灯
await dioOut.WriteAsync(false, CancellationToken.None); // 关灯

// 输入点
var dioIn = hardware.GetDigitalIO("DI_SENSOR");
bool sensor = await dioIn.ReadAsync(CancellationToken.None);
if (sensor)
{
    Console.WriteLine("传感器检测到物体");
}
```

---

### 3.4 IPlcCommunication - PLC 通信

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `PlcModel` (get) | PLC 型号 | 无 | `string` |
| `IsConnected` (get) | 是否已连接 | 无 | `bool` |
| `ConnectAsync(ct)` | 连接 PLC | `CancellationToken ct` | `Task` |
| `DisconnectAsync()` | 断开 PLC | 无 | `Task` |
| `ReadWordAsync(address, ct)` | 读字寄存器 | `string address, CancellationToken ct` | `Task<int>` |
| `WriteWordAsync(address, value, ct)` | 写字寄存器 | `string address, int value, CancellationToken ct` | `Task` |
| `ReadBitAsync(address, ct)` | 读位 | `string address, CancellationToken ct` | `Task<bool>` |
| `WriteBitAsync(address, value, ct)` | 写位 | `string address, bool value, CancellationToken ct` | `Task` |
| `ReadWordsAsync(startAddress, count, ct)` | 批量读字 | `string startAddress, int count, CancellationToken ct` | `Task<int[]>` |

#### 使用示例

```csharp
var hardware = ServiceLocator.GetService<IHardwareService>();
var plc = hardware.GetPlc();

// 连接
await plc.ConnectAsync(CancellationToken.None);

// 读 D 寄存器
int value = await plc.ReadWordAsync("D100", CancellationToken.None);

// 写 D 寄存器
await plc.WriteWordAsync("D101", 1234, CancellationToken.None);

// 读 M 位
bool running = await plc.ReadBitAsync("M100", CancellationToken.None);

// 写 M 位
await plc.WriteBitAsync("M101", true, CancellationToken.None);

// 批量读
int[] values = await plc.ReadWordsAsync("D0", 10, CancellationToken.None);
```

---

### 3.5 IVisionSystem - 视觉系统

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `VisionId` (get) | 视觉标识 | 无 | `string` |
| `CameraModel` (get) | 相机型号 | 无 | `string` |
| `IsConnected` (get) | 是否已连接 | 无 | `bool` |
| `ConnectAsync(ct)` | 连接相机 | `CancellationToken ct` | `Task<bool>` |
| `DisconnectAsync()` | 断开相机 | 无 | `Task` |
| `CaptureAsync(ct)` | 拍照 | `CancellationToken ct` | `Task<byte[]>` |
| `LocateAsync(recipe, image, ct)` | 定位 | `string recipe, byte[] image, CancellationToken ct` | `Task<VisionResult>` |
| `MatchAsync(recipe, image, ct)` | 匹配 | `string recipe, byte[] image, CancellationToken ct` | `Task<VisionResult>` |
| `ReadBarcodeAsync(image, ct)` | 读码 | `byte[] image, CancellationToken ct` | `Task<string>` |
| `GetCameraInfo()` | 获取相机信息 | 无 | `CameraInfo` |

#### 使用示例

```csharp
var hardware = ServiceLocator.GetService<IHardwareService>();
var vision = hardware.GetVisionSystem("CAM_1");

// 连接
await vision.ConnectAsync(CancellationToken.None);

// 拍照
byte[] image = await vision.CaptureAsync(CancellationToken.None);

// 定位
var result = await vision.LocateAsync("template_a", image, CancellationToken.None);
Console.WriteLine($"定位: X={result.X}, Y={result.Y}, 分数={result.Score}");

// 读码
string barcode = await vision.ReadBarcodeAsync(image, CancellationToken.None);
```

---

### 3.6 IBarcodeScanner - 扫码器

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `ScannerId` (get) | 扫码器标识 | 无 | `string` |
| `IsConnected` (get) | 是否已连接 | 无 | `bool` |
| `ScanAsync(timeoutMs, ct)` | 扫码 | `int timeoutMs, CancellationToken ct` | `Task<string>` |
| `ConnectAsync(ct)` | 连接 | `CancellationToken ct` | `Task<bool>` |
| `DisconnectAsync()` | 断开 | 无 | `Task` |

#### 使用示例

```csharp
var hardware = ServiceLocator.GetService<IHardwareService>();
var scanner = hardware.GetScanner("SCANNER_1");

// 扫码
string barcode = await scanner.ScanAsync(5000, CancellationToken.None);
if (!string.IsNullOrEmpty(barcode))
{
    Console.WriteLine($"扫码结果: {barcode}");
}
```

---

### 3.7 外设接口

#### IHeightGauge - 测高仪

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `MeasureAsync(ct)` | 测量高度 | `CancellationToken ct` | `Task<double>` |
| `ZeroAsync(ct)` | 清零 | `CancellationToken ct` | `Task` |
| `ConnectAsync(portName, baudRate)` | 连接 | `string portName, int baudRate` | `Task<bool>` |

#### ITemperatureController - 温控器

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `CurrentTemperature` (get) | 当前温度 | 无 | `double` |
| `SetPoint` (get/set) | 设定温度 | 无 | `double` |
| `IsAlarming` (get) | 是否报警 | 无 | `bool` |
| `ReadTemperatureAsync(ct)` | 读温度 | `CancellationToken ct` | `Task<double>` |
| `SetSetPointAsync(temp, ct)` | 设温度 | `double temp, CancellationToken ct` | `Task` |
| `StartAsync(ct)` | 启动 | `CancellationToken ct` | `Task` |
| `StopAsync(ct)` | 停止 | `CancellationToken ct` | `Task` |

#### IElectricProportionalValve - 电气比例阀

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `OutputPressure` (get/set) | 输出压力 (MPa) | 无 | `double` |
| `SetPressureAsync(MPa, ct)` | 设压力 | `double MPa, CancellationToken ct` | `Task` |
| `ReadPressureAsync(ct)` | 读压力 | `CancellationToken ct` | `Task<double>` |

#### IMicropressureSensor - 微压计

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `Pressure` (get) | 当前压力 | 无 | `double` |
| `ReadAsync(ct)` | 读压力 | `CancellationToken ct` | `Task<double>` |
| `ZeroAsync(ct)` | 清零 | `CancellationToken ct` | `Task` |

#### IExposureMeter - 曝光计

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `ReadIntensityAsync(ct)` | 读强度 (mW/cm²) | `CancellationToken ct` | `Task<double>` |
| `AccumulateAsync(duration, ct)` | 累积曝光 (mJ/cm²) | `TimeSpan duration, CancellationToken ct` | `Task<double>` |

#### ISpotAnalyzer - 光斑仪

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `AnalyzeAsync(ct)` | 分析光斑 | `CancellationToken ct` | `Task<SpotResult>` |

#### IScaleDevice - 称重设备

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `ReadWeightAsync(ct)` | 读重量 | `CancellationToken ct` | `Task<double>` |
| `ZeroAsync(ct)` | 清零 | `CancellationToken ct` | `Task` |

---

## 4. 业务服务 API

### 4.1 IProductionService - 生产服务

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `TotalCount` (get) | 总数量 | 无 | `int` |
| `OkCount` (get) | OK 数量 | 无 | `int` |
| `NgCount` (get) | NG 数量 | 无 | `int` |
| `UntestedCount` (get) | 未测试数量 | 无 | `int` |
| `YieldRate` (get) | 良率 (%) | 无 | `double` |
| `OEE` (get) | 设备综合效率 (%) | 无 | `double` |
| `RecordProduction(sn, result, testData)` | 记录生产 | `string serialNumber, string result, Dictionary<string, object> testData` | 无 |
| `ResetCount(userId, password)` | 重置计数 | `string userId, string password` | 无 |

**事件**: `OnProductionUpdated` - 生产数据更新

---

### 4.2 IGoldenSampleService - 封样服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `CreateSample(partNumber, sampleName, qrCode, testData)` | 创建封样 | `string partNumber, string sampleName, string qrCode, Dictionary<string, object> testData` | 无 |
| `AutoSample(partNumber, testCount, useAverage)` | 自动封样 | `string partNumber, int testCount, bool useAverage` | 无 |
| `ValidateSample(partNumber, testData, tolerance)` | 验证样品 | `string partNumber, Dictionary<string, object> testData, double tolerance` | `bool` |
| `GetSampleHistory(partNumber)` | 获取历史 | `string partNumber` | `IReadOnlyList<object>` |

---

### 4.3 ICheckListService - 点检服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `GetCheckList(partNumber)` | 获取点检清单 | `string partNumber` | `IReadOnlyList<CheckListItem>` |
| `Check(partNumber, userId)` | 执行点检 | `string partNumber, string userId` | `CheckResult` |
| `IsChecked(partNumber)` | 是否已点检 | `string partNumber` | `bool` |
| `ForceRecheck(partNumber)` | 强制重检 | `string partNumber` | 无 |
| `SetSchedule(partNumber, interval)` | 设点检计划 | `string partNumber, TimeSpan interval` | 无 |

---

### 4.4 IQualityService - 品质服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `RecordMeasurement(partNumber, itemName, value)` | 记录测量 | `string partNumber, string itemName, double value` | 无 |
| `CalculateStatistics(partNumber, itemName)` | 计算 SPC 统计 | `string partNumber, string itemName` | `SpcStatistics` |
| `GetDataPoints(partNumber, itemName, from, to)` | 获取历史数据 | `string partNumber, string itemName, DateTime from, DateTime to` | `IReadOnlyList<double>` |
| `EvaluateGrade(partNumber, itemName)` | 评估品质等级 | `string partNumber, string itemName` | `QualityGrade` |
| `CheckAlarm(partNumber, itemName)` | 检查 SPC 报警 | `string partNumber, string itemName` | `SpcAlarmResult` |
| `ClearData(partNumber)` | 清空数据 | `string partNumber` | 无 |

---

### 4.5 ITraceabilityService - 追溯服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `ValidateProductTrace(sn, partNumber, stationId)` | 验证追溯链 | `string serialNumber, string partNumber, string stationId` | `TraceResult` |
| `RecordStationPass(sn, stationId, operatorId)` | 记录过站 | `string serialNumber, string stationId, string operatorId` | `bool` |
| `GetStationHistory(sn)` | 获取过站历史 | `string serialNumber` | `StationHistory` |
| `CompareBatchCounts(batchNumber, expectedCount)` | 批次对比 | `string batchNumber, int expectedCount` | `BatchCompareResult` |
| `IsDuplicate(sn, stationId)` | 判断重码 | `string serialNumber, string stationId` | `bool` |

---

### 4.6 IMaintenanceService - 维保服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `GetPlan(deviceId)` | 获取维保计划 | `string deviceId` | `IReadOnlyList<MaintenanceItem>` |
| `CheckItem(itemId)` | 检查项目状态 | `string itemId` | `MaintenanceStatus` |
| `RecordExecution(itemId, userId, notes)` | 记录执行 | `string itemId, string userId, string notes` | 无 |
| `IsOverdue(itemId)` | 是否超期 | `string itemId` | `bool` |
| `AddCustomItem(itemId, name, frequency, countThreshold)` | 添加自定义条目 | `string itemId, string name, MaintenanceFrequency frequency, int countThreshold` | 无 |
| `UpdateAxisCount(axisId, operationCount)` | 更新轴次数 | `string axisId, int operationCount` | 无 |
| `UpdatePneumaticCount(componentId, operationCount)` | 更新气动件次数 | `string componentId, int operationCount` | 无 |
| `GetAlerts(deviceId)` | 获取预警 | `string deviceId` | `IReadOnlyList<MaintenanceAlert>` |

---

### 4.7 IExportService - 导出服务

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `ExportProductionDaily(date, outputPath)` | 导出生产日报 | `DateTime date, string outputPath` | `Task<ExportResult>` |
| `ExportAlarmsDaily(date, outputPath)` | 导出报警日报 | `DateTime date, string outputPath` | `Task<ExportResult>` |
| `ExportParameters(partNumber, outputPath)` | 导出参数 | `string partNumber, string outputPath` | `Task<ExportResult>` |
| `IsNetworkAvailable` (get) | 网络是否可用 | 无 | `bool` |
| `GetPendingExports()` | 获取待导出 | 无 | `IReadOnlyList<PendingExport>` |
| `FlushPendingExports()` | 刷新待导出 | 无 | `Task` |

---

## 5. 数据访问 API

### 5.1 IDatabaseProvider - 数据库提供器

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `Mode` (get) | 数据库模式 | 无 | `DatabaseMode` |
| `IsAvailable` (get) | 是否可用 | 无 | `bool` |
| `QueryAsync<T>(sql, param)` | 查询多行 | `string sql, object param` | `Task<IEnumerable<T>>` |
| `ExecuteAsync(sql, param)` | 执行命令 | `string sql, object param` | `Task<int>` |
| `QuerySingleAsync<T>(sql, param)` | 查询单行 | `string sql, object param` | `Task<T>` |

#### 使用示例

```csharp
var dbContext = ServiceLocator.GetService<DbContext>();
var provider = dbContext.Provider;

// 查询
var users = await provider.QueryAsync<User>(
    "SELECT * FROM Users WHERE Role = @role", 
    new { role = 2 });

// 执行
int rows = await provider.ExecuteAsync(
    "UPDATE Users SET LastLogin = @time WHERE UserId = @id",
    new { time = DateTime.Now, id = "user01" });

// 查询单行
var user = await provider.QuerySingleAsync<User>(
    "SELECT * FROM Users WHERE UserId = @id",
    new { id = "user01" });
```

---

### 5.2 仓储 API

| 仓储 | 接口 | 方法 | 说明 |
|------|------|------|------|
| 生产记录 | `IProductionRecordRepository` | `SaveStatsAsync(total, ok, ng, oee)` | 保存统计 |
| | | `LoadStatsAsync()` | 加载统计 |
| | | `ResetAsync()` | 重置 |
| 品质记录 | `IQualityRecordRepository` | `SaveMeasurementsAsync(pn, item, values)` | 保存测量 |
| | | `LoadMeasurementsAsync(pn, item)` | 加载测量 |
| | | `ClearAsync(pn)` | 清空 |
| 追溯记录 | `ITraceabilityRecordRepository` | `SaveHistoryAsync(sn, records)` | 保存历史 |
| | | `LoadHistoryAsync(sn)` | 加载历史 |
| | | `SaveDuplicateAsync(station, sns)` | 保存重码 |
| | | `LoadDuplicatesAsync(station)` | 加载重码 |
| 维保记录 | `IMaintenanceRecordRepository` | `SavePlanAsync(items)` | 保存计划 |
| | | `LoadPlanAsync()` | 加载计划 |
| 参数持久化 | `IParameterPersistenceRepository` | `LoadAsync<T>(pn, param)` | 加载参数 |
| | | `SaveAsync<T>(pn, param, data)` | 保存参数 |
| | | `GetChangeLogsAsync(pn, param)` | 获取履历 |

---

## 6. IFMS 通信 API

### IIfmsBroker

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `IsConnected` (get) | 是否已连接 | 无 | `bool` |
| `IsEnabled` (get) | 是否启用 | 无 | `bool` |
| `ValidateStationAsync(req)` | 验证工站 | `StationValidationRequest req` | `Task<StationValidationResult>` |
| `ValidatePartNumberAsync(pn, deviceId)` | 验证件号 | `string partNumber, string deviceId` | `Task<bool>` |
| `UploadProductionDataAsync(data)` | 上传生产数据 | `ProductionDataUpload data` | `Task<bool>` |
| `UploadDeviceStatusAsync(status)` | 上传设备状态 | `DeviceStatusSnapshot status` | `Task<bool>` |
| `UploadAlarmRecordAsync(alarm)` | 上传报警 | `AlarmUploadData alarm` | `Task<bool>` |
| `UploadParameterSnapshotAsync(pn, params)` | 上传参数 | `string partNumber, object parameters` | `Task<bool>` |
| `UploadQualityDataAsync(report)` | 上传品质 | `QualityReportData report` | `Task<bool>` |
| `FlushQueueAsync()` | 刷新离线队列 | 无 | `Task` |

#### 使用示例

```csharp
var ifms = ServiceLocator.GetService<IIfmsBroker>();

// 验证工站
var req = new StationValidationRequest
{
    DeviceId = "MTF_NGL13",
    MacAddress = "00-1A-2B-3C-4D-5E",
    SoftwareVersion = "v3.6",
    PartNumber = "ABC123",
    UserId = "user01"
};
var result = await ifms.ValidateStationAsync(req);
if (!result.IsValid)
{
    Console.WriteLine($"校验失败: {string.Join(", ", result.FailedChecks)}");
}

// 上传生产数据
var prodData = new ProductionDataUpload
{
    SerialNumber = "SN123456",
    PartNumber = "ABC123",
    StationId = "MTF_NGL13",
    Result = "OK",
    TestData = new Dictionary<string, object> { ["Exposure"] = 10.5 },
    TestTime = DateTime.Now
};
await ifms.UploadProductionDataAsync(prodData);

// 刷新离线队列
await ifms.FlushQueueAsync();
```

---

## 7. 设备适配 API

### IDevice - 设备接口

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `DeviceType` (get) | 设备类型标识 | 无 | `string` |
| `DeviceName` (get) | 设备名称 | 无 | `string` |
| `Version` (get) | 版本号 | 无 | `string` |
| `Initialize()` | 初始化 | 无 | `bool` |
| `Start()` | 启动 | 无 | `bool` |
| `Stop()` | 停止 | 无 | `bool` |
| `Shutdown()` | 关闭 | 无 | `bool` |
| `GetStatus()` | 获取状态 | 无 | `DeviceStatus` |
| `GetParameters()` | 获取参数 | 无 | `DeviceParameters` |
| `SetParameters(params)` | 设置参数 | `DeviceParameters` | `bool` |
| `ExecuteAction(actionName, parameters)` | 执行动作 | `string actionName, Dictionary<string, object> parameters` | `bool` |
| `GetTestData()` | 获取测试数据 | 无 | `Dictionary<string, object>` |
| `GetAvailableActions()` | 获取可用动作 | 无 | `List<string>` |
| `SingleStepExecute(action)` | 单步执行 | `string action` | `bool` |

**事件**: `StatusChanged` - 设备状态变化

### DeviceFactory - 设备工厂

```csharp
// 创建设备
var device = DeviceFactory.Create("MTFTest");
device.Initialize();
device.Start();

// 获取所有可用设备类型
var deviceTypes = DeviceFactory.GetAvailableTypes();  // ["MTFTest", "ZG13", ...]
```

---

## 8. 脚本引擎 API (Lua)

### 8.1 LuaScriptEngine

| 方法 | 说明 | 参数 | 返回值 |
|------|------|------|--------|
| `ResetScript()` | 重置脚本 | 无 | 无 |
| `LoadScript(script)` | 加载脚本 | `string script` | 无 |
| `RunAsync()` | 运行脚本 | 无 | `Task` |
| `Pause()` | 暂停 | 无 | 无 |
| `Resume()` | 恢复 | 无 | 无 |
| `StepAsync()` | 单步执行 | 无 | `Task` |
| `Stop()` | 停止 | 无 | 无 |
| `Evaluate(expression)` | 计算表达式 | `string expression` | `ScriptObject` |

**事件**:
- `OnOutput(string)` - 输出
- `OnError(string)` - 异常
- `OnStopped` - 停止
- `OnPaused` - 暂停
- `OnLineChanged(int)` - 行变化

### 8.2 Lua 硬件函数（28 个绑定）

| 类别 | Lua 函数 | C# 绑定 | 说明 |
|------|----------|---------|------|
| 轴 | `axis_move_abs(id, pos)` | AxisMoveAbs | 绝对运动 |
| | `axis_move_rel(id, delta)` | AxisMoveRel | 相对运动 |
| | `axis_home(id)` | AxisHome | 回原点 |
| | `axis_wait_done(id, timeout)` | AxisWaitStop | 等待轴停止 |
| | `axis_get_pos(id)` | AxisGetPos | 获取位置 |
| | `axis_stop(id)` | AxisStop | 停止 |
| DIO | `io_set(idx, val)` | DioWriteOutput | IO 置位 |
| | `io_get(idx)` | DioReadInput | IO 读取 |
| PLC | `plc_read_d(addr)` | PlcReadD | 读 D 寄存器 |
| | `plc_write_d(addr, val)` | PlcWriteD | 写 D 寄存器 |
| | `plc_read_m(addr)` | PlcReadM | 读 M 继电器 |
| | `plc_write_m(addr, val)` | PlcWriteM | 写 M 继电器 |
| 扫码 | `scanner_read(id, timeout)` | ScannerRead | 扫码 |
| | `scanner_trigger(id)` | ScannerTrigger | 触发扫码 |
| 视觉 | `vision_capture(id, recipe)` | CameraCapture | 拍照 |
| | `vision_locate(id, recipe)` | VisionLocate | 定位 |
| | `vision_read_code(id)` | VisionReadCode | 读码 |
| 传感器 | `sensor_read(id)` | SensorRead | 传感器读数 |
| | `scale_read(id)` | ScaleRead | 称重 |
| | `temperature_read(id)` | TemperatureRead | 温度 |
| | `pressure_read(id)` | PressureRead | 压力 |
| 上报 | `mes_upload(task, data)` | SendToIfms | 上报 IFMS |
| | `log_to_db(category, msg)` | LogToDb | 日志 |

### 8.3 Lua 脚本示例

```lua
-- 初始化
log("INFO", "开始初始化")
axis_home("X")
axis_home("Y")
axis_home("Z")

-- 扫码
local barcode = scan_barcode("SCANNER_1", 5000)
globals:set("CurrentBarcode", barcode)

-- 轴运动
axis_move_abs("Z", 100.0, 50.0)
axis_wait_done("Z", 10000)

-- IO 操作
io_set("DO_LAMP", true)
io_wait("DI_SENSOR", true, 5000)

-- 视觉
vision_capture("CAM_1", "recipe_a")
local score = vision_locate("CAM_1", "template_a")

-- 条件判断
if score > 0.8 then
    log("INFO", "定位成功")
    mes_upload("DataReport", { StationId = "ST01", Barcode = barcode, Result = "OK" })
    return true  -- 完成分支
else
    log("ERROR", "定位失败")
    return false  -- 失败分支
end
```

### 8.4 Lua → 工作流分支映射

| Lua 返回值 | 工作流走向 |
|-----------|-----------|
| `return true` / `return 1` | → 完成 |
| `return false` / `return 0` | → 失败 |
| `return 2` | → 分支 A |
| `return 3` | → 分支 B |

---

## 9. 工作流引擎 API

### 9.1 WorkflowBuilder - 链式构建器

```csharp
var builder = new WorkflowBuilder("WF_LeakTest", "泄露测试工作流", "XL07");
var def = builder
    .Start("上料完成")
    .ScannerRead("scan_barcode", 3000)
    .AxisMove("Z", 0, 30)
    .Delay("等待稳定", 500)
    .DioWrite("加压阀", 0, true)
    .WaitFor("pressure_ok", 10000)
    .LuaScript("log('INFO', '测试中...')")
    .DioWrite("加压阀", 0, false)
    .AxisMove("Z", 10, 30)
    .End("测试完成")
    .Build();
```

### 9.2 WorkflowEngine - 执行引擎

| 方法/属性 | 说明 | 参数 | 返回值 |
|-----------|------|------|--------|
| `RunAsync(ct)` | 运行工作流 | `CancellationToken ct` | `Task` |
| `Definition` (get/set) | 工作流定义 | 无 | `WorkflowDefinition` |

### 9.3 节点类型

| 分类 | 节点 |
|------|------|
| 流程控制 | Start, End, Delay, Condition, LoopStart, LoopEnd, Parallel, Goto |
| Lua 脚本 | LuaScript |
| 轴控制 | AxisMove, AxisHome, AxisStop |
| IO 控制 | DioWrite, DioRead, WaitFor |
| 扫码视觉 | ScannerRead, VisionCapture, VisionLocate |
| PLC 通信 | PlcRead, PlcWrite, PlcWait |
| MES 交互 | DataReport, TaskDownload, QueryInfo |

---

## 10. 数据模型

### 10.1 核心数据模型

| 类 | 说明 | 主要属性 |
|----|------|----------|
| `LoginResult` | 登录结果 | Success, UserId, UserName, Role, ErrorMessage |
| `DeviceParameters` | 设备参数 | PartNumber, CalibrationParams, ProcessParams, InfoParams |
| `ParameterChangeLog` | 参数变更履历 | Id, PartNumber, ParamName, OldValue, NewValue, ChangedBy, ChangedTime |
| `FlowAction` | 流程动作 | Code, Name, Description, Category, Execute |
| `TimedTaskStatus` | 定时任务状态 | TaskName, LastRunTime, NextRunTime, IsRunning, LastError |
| `StationValidationRequest` | 工站校验请求 | DeviceId, LineId, MacAddress, SoftwareVersion, PartNumber, UserId, OrderNumber |
| `StationValidationResult` | 工站校验结果 | IsValid, FailedChecks, Message |
| `DeviceStatusSnapshot` | 设备状态快照 | DeviceId, DeviceType, Status, PartNumber, Timestamp, OEE |
| `ProductionDataUpload` | 生产数据上传 | SerialNumber, PartNumber, StationId, Result, TestData, TestTime |
| `QualityReportData` | 品质报告 | DeviceId, PartNumber, CPK, PPK, ReportTime |
| `AlarmUploadData` | 报警上传 | AlarmCode, Level, Description, OccurTime, IsActive |
| `TrayStateMachine` | 料仓状态机 | StorageId, LayerIndex, State, TotalSlots, FinishedCount, OkCount, NgCount, BatchNumber |
| `SpcStatistics` | SPC 统计 | Mean, StdDev, CPK, PPK, USL, LSL, Min, Max, Range, SampleCount, RawData |
| `TraceResult` | 追溯结果 | IsValid, SerialNumber, PartNumber, StationId, PassedStations, MissingStation, IsDuplicate |
| `StationHistory` | 过站历史 | SerialNumber, Records |
| `MaintenanceItem` | 维保项目 | Id, Name, Frequency, CountThreshold, CurrentCount, LastExecution, NextDueDate |
| `ExportResult` | 导出结果 | Success, FilePath, ErrorMessage, RecordCount, FileSizeBytes |

### 10.2 视觉/扫码数据模型

| 类 | 说明 | 主要属性 |
|----|------|----------|
| `VisionResult` | 视觉结果 | Success, Score, X, Y, Angle, Barcode, ExtraData |
| `CameraInfo` | 相机信息 | Model, SerialNumber, Width, Height, FrameRate |
| `SpotResult` | 光斑结果 | CentroidX, CentroidY, Diameter, Ellipticity |

### 10.3 仿真数据模型

| 类 | 说明 |
|----|------|
| `SimDataStore` | 仿真数据存储（单例） |
| `SimAxisData` | 仿真轴数据 |
| `SimDioData` | 仿真 DIO 数据 |

---

## 11. 枚举类型

### 11.1 状态枚举

| 枚举 | 值 | 说明 |
|------|-----|------|
| `MachineStatus` | Idle, Running, Paused, Stopped, Error, Alarm, Maintenance, Checking, Debugging | 机台状态 |
| `DeviceStatus` | Idle, Running, Paused, Error, Maintenance, Checking | 设备状态 |
| `FlowState` | Idle, Running, Paused, Stopping, Error | 流程状态 |
| `TrayState` | Empty(0), Pending(1), Confirmed(2), PendingWork(3), Working(4), Complete(5) | 料仓状态 |

### 11.2 配置枚举

| 枚举 | 值 | 说明 |
|------|-----|------|
| `DatabaseMode` | SqlServerOnly, AccessOnly, SqlServerWithAccessFallback | 数据库模式 |
| `ParameterPersistenceMode` | JsonFile, Database, JsonWithDbSync | 参数持久化模式 |
| `SimulationMode` | None, Full, Partial, Replay | 仿真模式 |
| `AnalogType` | Voltage_0_10V, Current_4_20mA | 模拟量类型 |
| `ModbusType` | RTU, TCP | Modbus 类型 |

### 11.3 业务枚举

| 枚举 | 值 | 说明 |
|------|-----|------|
| `AlarmLevel` | Error(1), Tip(2), Trace(3) | 报警等级 |
| `UserRole` | Operator(1), Technician(2), Engineer(3), Administrator(4), Maintainer(5), DynamicPassword(6), USBKey(7) | 用户角色 |
| `FlowCategory` | Shutdown(-3), PartInit(-2), HardwareInit(-1), UIOperation(1-99), Debug(100-199), Reset(200-299), AutoRun(300+) | 流程分类 |
| `QualityGrade` | Unknown, Poor, Acceptable, Good, Excellent | 品质等级 |
| `SpcAlarmLevel` | Normal, Warning, StopProduction | SPC 报警等级 |
| `MaintenanceFrequency` | Daily, Weekly, Monthly, Quarterly, ByOperationCount, ByMileage, Custom | 维保频率 |
| `MaintenanceAlertLevel` | Info, Warning, Critical | 维保预警等级 |
| `CheckItemCategory` | Safety, Hardware, Calibration, Environment, Consumable | 点检分类 |
| `ExportType` | Production, Alarms, Parameters, Quality | 导出类型 |

---

## 附录：常见错误处理

### 1. ServiceProvider 未初始化

```
InvalidOperationException: ServiceProvider 尚未初始化，请先调用 AppBootstrap.Initialize()。
```

**原因**: 在 AppBootstrap 初始化前调用 ServiceLocator.GetService<T>()。

**解决**: 确保在 Program.cs Main 方法中先调用 `bootstrap.Initialize()` 再使用服务。

---

### 2. 硬件仿真模式切换

```csharp
// Program.cs
var bootstrap = new AppBootstrap
{
    Mode = RuntimeMode.Simulation  // Simulation 或 Real
};
bootstrap.Initialize();
```

---

### 3. 线程安全注意事项

- **UI 线程**: 所有 WinForms 控件操作必须在 STA 线程
- **跨线程调用**: 使用 `SynchronizationContext.Post()` 编组到 UI 线程
- **共享状态**: 使用 `ReaderWriterLockSlim` / `lock` / `SemaphoreSlim` 保护
- **后台线程**: 所有可能阻塞的异步方法必须接受 `CancellationToken`

---

## 相关文档

- [CLAUDE.md](./CLAUDE.md) - Claude Code 操作指南
- [README.md](./README.md) - 项目快速开始
- [平台化软件开发方案.md](../平台化软件开发方案.md) - 完整技术方案
- [代码架构说明.md](../代码架构说明.md) - 详细架构说明

---

> **Aether 设备软件平台** — API 文档 v3.6
