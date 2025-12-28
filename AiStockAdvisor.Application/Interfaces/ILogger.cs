using System;

namespace AiStockAdvisor.Application.Interfaces
{
    /// <summary>
    /// 定義日誌記錄的通用介面。
    /// </summary>
    public interface ILogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message, Exception ex = null);
    }
}
