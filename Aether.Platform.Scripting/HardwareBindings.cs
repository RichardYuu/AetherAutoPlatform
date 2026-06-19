using System;
using MoonSharp.Interpreter;

namespace Aether.Platform.Scripting
{
    /// <summary>
    /// 硬件操作绑定 — 将 C# 硬件函数注册为 Lua 全局函数
    /// 实际硬件注入由外部调用方通过 SetDelegate 完成
    /// </summary>
    public class HardwareBindings
    {
        /// <summary>静态全局实例，供 UI 层注入到脚本编辑器</summary>
        public static HardwareBindings Current { get; set; }

        // 回调委托 — 外部注入具体实现

        public Func<int, int, int, bool> AxisMoveTo { get; set; }           // axisId, targetPos(um), speed(mm/s) → ok
        public Func<int, int> AxisGetPos { get; set; }                      // axisId → position(um)
        public Func<int, bool> AxisHome { get; set; }                       // axisId → ok
        public Func<int, bool> AxisStop { get; set; }                       // axisId → ok
        public Func<int, int, bool> AxisMoveAbs { get; set; }               // axisId, pos(um) → ok
        public Func<int, int, bool> AxisMoveRel { get; set; }               // axisId, delta(um) → ok
        public Func<int, int, bool> AxisWaitStop { get; set; }              // axisId, timeoutMs → ok

        // DIO（含分离的 Input/Output 风格）
        public Func<int, bool> DioRead { get; set; }                        // ioIndex → state
        public Action<int, bool> DioWrite { get; set; }                     // ioIndex, state
        public Func<int, bool> DioReadInput { get; set; }                   // ioIndex → state
        public Func<int, bool> DioReadOutput { get; set; }                  // ioIndex → state
        public Func<int, bool, bool> DioWriteOutput { get; set; }           // ioIndex, state → ok
        public Func<int, bool> DioToggleOutput { get; set; }                // ioIndex → ok

        // PLC
        public Func<string, int> PlcReadD { get; set; }                     // address → value
        public Action<string, int> PlcWriteD { get; set; }                  // address, value
        public Func<string, bool> PlcReadM { get; set; }                    // address → bit
        public Action<string, bool> PlcWriteM { get; set; }                 // address, bit

        // 相机 / 扫码器
        public Func<string> VisionCapture { get; set; }                     // → result json
        public Func<string, string> VisionLocate { get; set; }              // modelName → position json
        public Func<string, string, string> VisionReadCode { get; set; }    // modelName, codeType → code
        public Func<string, string> CameraCapture { get; set; }             // cameraId → result json

        public Func<string> ScannerRead { get; set; }                       // → barcode
        public Func<string> ScannerTrigger { get; set; }                    // → barcode
        public Action<string, string> ScannerSend { get; set; }             // command, data

        // 传感器
        public Func<double> ScaleRead { get; set; }                         // → grams
        public Action ScaleTare { get; set; }
        public Func<double> TemperatureRead { get; set; }                   // → ℃
        public Func<double> PressureRead { get; set; }                      // → kPa
        public Func<int, double> SensorRead { get; set; }                   // sensorId → value

        // 数据上报
        public Action<string, string> SendToIfms { get; set; }              // tableName, jsonData
        public Action<string, string, string> LogToDb { get; set; }         // level, category, message

        /// <summary>将全部硬件函数注册到 Lua 全局作用域</summary>
        public void RegisterToScript(Script script)
        {
            if (script == null) return;

            // 轴
            if (AxisMoveTo != null)
                script.Globals["axis_move"] = (Func<int, int, int, bool>)AxisMoveTo;
            else if (AxisMoveAbs != null)
                script.Globals["axis_move"] = (Func<int, int, bool>)((id, pos) => AxisMoveAbs(id, pos));

            if (AxisGetPos != null)     script.Globals["axis_pos"]      = (Func<int, int>)AxisGetPos;
            if (AxisHome != null)       script.Globals["axis_home"]     = (Func<int, bool>)AxisHome;
            if (AxisStop != null)       script.Globals["axis_stop"]     = (Func<int, bool>)AxisStop;
            if (AxisMoveAbs != null)    script.Globals["axis_move_abs"] = (Func<int, int, bool>)AxisMoveAbs;
            if (AxisMoveRel != null)    script.Globals["axis_move_rel"] = (Func<int, int, bool>)AxisMoveRel;

            // DIO
            var dioRead = DioReadInput ?? DioRead;
            if (dioRead != null)        script.Globals["dio_read"]      = (Func<int, bool>)dioRead;
            var dioWriteFn = DioWriteOutput;
            if (dioWriteFn != null)     script.Globals["dio_write"]     = (Func<int, bool, bool>)dioWriteFn;
            else if (DioWrite != null)  script.Globals["dio_write"]     = (Action<int, bool>)DioWrite;

            // PLC
            if (PlcReadD != null)       script.Globals["plc_read_d"]    = (Func<string, int>)PlcReadD;
            if (PlcWriteD != null)      script.Globals["plc_write_d"]   = (Action<string, int>)PlcWriteD;
            if (PlcReadM != null)       script.Globals["plc_read_m"]    = (Func<string, bool>)PlcReadM;
            if (PlcWriteM != null)      script.Globals["plc_write_m"]   = (Action<string, bool>)PlcWriteM;

            // 相机
            if (CameraCapture != null)
                script.Globals["camera_capture"] = CameraCapture;
            else if (VisionCapture != null)
                script.Globals["camera_capture"] = (Func<string, string>)((id) => VisionCapture());
            if (VisionLocate != null)    script.Globals["vision_locate"]  = (Func<string, string>)VisionLocate;
            if (VisionReadCode != null)  script.Globals["vision_read_code"] = (Func<string, string, string>)VisionReadCode;

            // 扫码
            var scanFn = ScannerTrigger ?? ScannerRead;
            if (scanFn != null)         script.Globals["scanner_read"]  = (Func<string>)scanFn;
            if (ScannerSend != null)    script.Globals["scanner_send"]  = (Action<string, string>)ScannerSend;

            // 传感器
            if (ScaleRead != null)      script.Globals["scale_read"]    = (Func<double>)ScaleRead;
            if (ScaleTare != null)      script.Globals["scale_tare"]    = (Action)ScaleTare;
            if (PressureRead != null)   script.Globals["pressure_read"] = (Func<double>)PressureRead;
            if (TemperatureRead != null) script.Globals["temp_read"]    = (Func<double>)TemperatureRead;
            if (SensorRead != null)     script.Globals["sensor_read"]   = (Func<int, double>)SensorRead;

            // 数据
            if (SendToIfms != null)     script.Globals["send_to_ifms"]  = (Action<string, string>)SendToIfms;
            if (LogToDb != null)        script.Globals["log_to_db"]     = (Action<string, string, string>)LogToDb;
        }
    }
}
