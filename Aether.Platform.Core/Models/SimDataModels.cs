using System;
using System.Collections.Generic;

namespace Aether.Platform.Core.Models
{
    /// <summary>
    /// 仿真数据存储 — 集中管理所有可手动修改的仿真数值
    /// 仿真硬件读取此处数据，SimDataEditorView 写入此处数据
    /// </summary>
    public class SimDataStore
    {
        public static SimDataStore Instance { get; } = new SimDataStore();

        // 轴数据（6 轴）
        public Dictionary<string, SimAxisData> Axes { get; } = new Dictionary<string, SimAxisData>
        {
            ["X"] = new SimAxisData { Name = "X轴", Position = 0, Speed = 50, IsEnabled = true },
            ["Y"] = new SimAxisData { Name = "Y轴", Position = 0, Speed = 50, IsEnabled = true },
            ["Z"] = new SimAxisData { Name = "Z轴", Position = 0, Speed = 30, IsEnabled = true },
            ["R"] = new SimAxisData { Name = "R轴(旋转)", Position = 0, Speed = 20, IsEnabled = true },
            ["U"] = new SimAxisData { Name = "U轴", Position = 0, Speed = 50, IsEnabled = true },
            ["V"] = new SimAxisData { Name = "V轴", Position = 0, Speed = 50, IsEnabled = true },
        };

        // 数字 IO（16 入 + 16 出）
        public Dictionary<int, SimDioData> Inputs { get; } = new Dictionary<int, SimDioData>();
        public Dictionary<int, SimDioData> Outputs { get; } = new Dictionary<int, SimDioData>();

        // PLC D 寄存器
        public Dictionary<string, int> PlcDRegisters { get; } = new Dictionary<string, int>
        {
            ["D0"] = 0,  ["D100"] = 0,  ["D200"] = 0,  ["D300"] = 0,
            ["D400"] = 0, ["D500"] = 0,  ["D1000"] = 0,
        };

        // PLC M 继电器
        public Dictionary<string, bool> PlcMBits { get; } = new Dictionary<string, bool>
        {
            ["M0"] = false, ["M1"] = false, ["M2"] = false, ["M10"] = false, ["M20"] = false,
        };

        // 传感器值
        public Dictionary<string, double> SensorValues { get; } = new Dictionary<string, double>
        {
            ["温度1"] = 25.0,    ["温度2"] = 25.0,
            ["压力1"] = 0.0,     ["压力2"] = 0.0,
            ["真空度"] = -60.0,
            ["重量"] = 0.0,
            ["比例阀"] = 50.0,
            ["曝光值"] = 0.0,
            ["测高"] = 0.0,
        };

        // 扫码器
        public double ScannerSuccessRate { get; set; } = 0.98;
        public string NextBarcode { get; set; } = "";

        // 视觉
        public double VisionScore { get; set; } = 0.95;
        public double VisionX { get; set; } = 100;
        public double VisionY { get; set; } = 100;
        public double VisionAngle { get; set; } = 0;

        // 模拟量通道
        public Dictionary<int, double> AnalogChannels { get; } = new Dictionary<int, double>
        {
            [0] = 0.0, [1] = 0.0, [2] = 0.0, [3] = 0.0,
            [4] = 0.0, [5] = 0.0, [6] = 0.0, [7] = 0.0,
        };

        public SimDataStore()
        {
            for (int i = 0; i < 16; i++)
            {
                Inputs[i] = new SimDioData { Index = i, Name = $"输入 {i}", Value = false };
                Outputs[i] = new SimDioData { Index = i, Name = $"输出 {i}", Value = false };
            }
        }
    }

    public class SimAxisData
    {
        public string Name { get; set; }
        public double Position { get; set; }
        public double Speed { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsHomed { get; set; } = true;
        public bool IsMoving { get; set; }
    }

    public class SimDioData
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool Value { get; set; }
    }
}