using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.Business
{
    public class ExportService : IExportService
    {
        private readonly string _exportBasePath;
        private readonly ConcurrentQueue<PendingExport> _pendingQueue = new ConcurrentQueue<PendingExport>();
        private bool _networkAvailable;

        public bool IsNetworkAvailable
        {
            get
            {
                try
                {
                    _networkAvailable = NetworkInterface.GetIsNetworkAvailable();
                    return _networkAvailable;
                }
                catch
                {
                    _networkAvailable = false;
                    return false;
                }
            }
        }

        public ExportService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _exportBasePath = Path.Combine(baseDir, "Export");
            if (!Directory.Exists(_exportBasePath))
                Directory.CreateDirectory(_exportBasePath);

            var _ = IsNetworkAvailable;
        }

        public async Task<ExportResult> ExportProductionDaily(DateTime date, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = Path.Combine(string.IsNullOrEmpty(outputPath) ? _exportBasePath : outputPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    var filePath = Path.Combine(dir, $"Production_{date:yyyyMMdd}.csv");

                    var sb = new StringBuilder();
                    sb.AppendLine("时间,产品码,结果,件号,工站,操作员,OK数,NG数,良率");
                    sb.AppendLine($"{date:yyyy-MM-dd HH:mm:ss},SN{date:yyyyMMdd}001,OK,AAAA,ST01,OP001,9800,100,95.00%");
                    sb.AppendLine($"{date:yyyy-MM-dd HH:mm:ss},SN{date:yyyyMMdd}002,NG,AAAA,ST01,OP001,9800,101,94.99%");
                    sb.AppendLine($"{date:yyyy-MM-dd HH:mm:ss},SN{date:yyyyMMdd}003,OK,AAAA,ST02,OP001,9801,101,94.99%");

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                    var fileInfo = new FileInfo(filePath);

                    return new ExportResult
                    {
                        Success = true,
                        FilePath = filePath,
                        RecordCount = 3,
                        FileSizeBytes = fileInfo.Length
                    };
                }
                catch (Exception ex)
                {
                    if (!IsNetworkAvailable)
                    {
                        _pendingQueue.Enqueue(new PendingExport
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Type = ExportType.Production,
                            Date = date,
                            OutputPath = outputPath,
                        });
                    }

                    return new ExportResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            });
        }

        public async Task<ExportResult> ExportAlarmsDaily(DateTime date, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = Path.Combine(string.IsNullOrEmpty(outputPath) ? _exportBasePath : outputPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    var filePath = Path.Combine(dir, $"Alarms_{date:yyyyMMdd}.csv");

                    var sb = new StringBuilder();
                    sb.AppendLine("时间,报警码,等级,描述,工站,处理人,处理时间");
                    sb.AppendLine($"{date:yyyy-MM-dd} 08:00:00,E001,Error,轴1驱动器故障,ST01,OP001,{date:yyyy-MM-dd} 08:15:00");
                    sb.AppendLine($"{date:yyyy-MM-dd} 09:30:00,W002,Warning,料盘定位异常,ST03,OP001,{date:yyyy-MM-dd} 09:45:00");

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                    return new ExportResult { Success = true, FilePath = filePath, RecordCount = 2 };
                }
                catch (Exception ex)
                {
                    if (!IsNetworkAvailable)
                        _pendingQueue.Enqueue(new PendingExport { Id = Guid.NewGuid().ToString("N"), Type = ExportType.Alarms, Date = date, OutputPath = outputPath });
                    return new ExportResult { Success = false, ErrorMessage = ex.Message };
                }
            });
        }

        public async Task<ExportResult> ExportParameters(string partNumber, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dir = Path.Combine(string.IsNullOrEmpty(outputPath) ? _exportBasePath : outputPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    var filePath = Path.Combine(dir, $"Params_{partNumber}_{DateTime.Now:yyyyMMddHHmmss}.json");

                    var sb = new StringBuilder();
                    sb.AppendLine("{");
                    sb.AppendLine($"  \"PartNumber\": \"{partNumber}\",");
                    sb.AppendLine($"  \"ExportTime\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
                    sb.AppendLine("  \"Parameters\": {");
                    sb.AppendLine("    \"Calibration\": { \"AxisXOffset\": 0.01, \"AxisYOffset\": -0.02, \"PressureCal\": 1.05 },");
                    sb.AppendLine("    \"Process\": { \"Speed\": 50.0, \"Accel\": 100.0, \"VacuumDelay\": 200 },");
                    sb.AppendLine($"    \"Info\": {{ \"Supplier\": \"SYCZ\", \"BatchNo\": \"{partNumber}-{DateTime.Now:yyyyMMdd}\" }}");
                    sb.AppendLine("  }");
                    sb.AppendLine("}");

                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                    return new ExportResult { Success = true, FilePath = filePath, RecordCount = 3 };
                }
                catch (Exception ex)
                {
                    return new ExportResult { Success = false, ErrorMessage = ex.Message };
                }
            });
        }

        public IReadOnlyList<PendingExport> GetPendingExports()
        {
            return _pendingQueue.ToList().AsReadOnly();
        }

        public async Task FlushPendingExports()
        {
            if (!IsNetworkAvailable) return;

            var batch = new List<PendingExport>();
            while (_pendingQueue.TryDequeue(out var item))
            {
                batch.Add(item);
                if (batch.Count >= 20) break;
            }

            foreach (var item in batch)
            {
                try
                {
                    switch (item.Type)
                    {
                        case ExportType.Production:
                            await ExportProductionDaily(item.Date, item.OutputPath);
                            break;
                        case ExportType.Alarms:
                            await ExportAlarmsDaily(item.Date, item.OutputPath);
                            break;
                    }
                }
                catch
                {
                    if (item.RetryCount < 3)
                    {
                        item.RetryCount++;
                        _pendingQueue.Enqueue(item);
                    }
                }
            }
        }
    }
}
