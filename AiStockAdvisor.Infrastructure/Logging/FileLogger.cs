using System;
using System.IO;
using AiStockAdvisor.Application.Interfaces;
using Serilog;

namespace AiStockAdvisor.Infrastructure.Logging
{
    /// <summary>
    /// 實作 ILogger 介面，使用 Serilog 作為日誌核心。
    /// 支援 Console 輸出與檔案滾動 (Rolling File)。
    /// </summary>
    public class FileLogger : AiStockAdvisor.Application.Interfaces.ILogger, IDisposable
    {
        private readonly Serilog.ILogger _logger;

        public FileLogger(string logDirectory = "Logs")
        {
            string absoluteLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectory);
            string logPath = Path.Combine(absoluteLogDir, "log-.txt");

            // Configure Serilog
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(path: logPath,
                              rollingInterval: RollingInterval.Day,
                              retainedFileCountLimit: 30, // Keep last 30 days
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
                
            _logger.Information($"Logger initialized. Logs will be stored in: {absoluteLogDir}");
        }

        public void LogInformation(string message)
        {
            _logger.Information(message);
        }

        public void LogWarning(string message)
        {
            _logger.Warning(message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (ex != null)
                _logger.Error(ex, message);
            else
                _logger.Error(message);
        }
        
        public void Dispose()
        {
            Log.CloseAndFlush();
        }
    }
}
