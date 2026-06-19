using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.Platform.Core.Models
{
    public enum MachineStatus
    {
        Idle,
        Running,
        Paused,
        Stopped,
        Error,
        Alarm,
        Maintenance,
        Checking,
        Debugging
    }

    public enum DeviceStatus
    {
        Idle,
        Running,
        Paused,
        Error,
        Maintenance,
        Checking
    }

    public enum AlarmLevel
    {
        Error = 1,
        Tip = 2,
        Trace = 3
    }

    public enum DatabaseMode
    {
        SqlServerOnly,
        AccessOnly,
        SqlServerWithAccessFallback
    }

    public enum ParameterPersistenceMode
    {
        JsonFile,
        Database,
        JsonWithDbSync
    }

    public enum SimulationMode
    {
        None,
        Full,
        Partial,
        Replay
    }

    public enum FlowState
    {
        Idle,
        Running,
        Paused,
        Stopping,
        Error
    }

    public enum FlowCategory
    {
        Shutdown = -3,
        PartInit = -2,
        HardwareInit = -1,
        UIOperation = 1,
        Debug = 100,
        Reset = 200,
        AutoRun = 300
    }

    public enum TrayState
    {
        Empty = 0,
        Pending = 1,
        Confirmed = 2,
        PendingWork = 3,
        Working = 4,
        Complete = 5
    }

    public enum UserRole
    {
        Operator = 1,
        Technician = 2,
        Engineer = 3,
        Administrator = 4,
        Maintainer = 5,
        DynamicPassword = 6,
        USBKey = 7
    }

    public enum AnalogType
    {
        Voltage_0_10V,
        Current_4_20mA
    }

    public enum ModbusType
    {
        RTU,
        TCP
    }

    public enum MaterialStage
    {
        Normal,
        LowWarning,
        TailProcessing,
        Exhausted
    }
}