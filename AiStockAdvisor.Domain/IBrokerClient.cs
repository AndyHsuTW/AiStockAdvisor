using System;

namespace AiStockAdvisor.Domain
{
    /// <summary>
    /// 定義與證券經紀商服務互動的契約。
    /// 包含驗證與資料訂閱功能。
    /// </summary>
    public interface IBrokerClient
    {
        /// <summary>
        /// 驗證並登入經紀商系統。
        /// </summary>
        /// <param name="username">帳號</param>
        /// <param name="password">密碼</param>
        void Login(string username, string password);

        /// <summary>
        /// 當接收到即時成交數據時觸發的事件。
        /// </summary>
        event Action<Tick> OnTickReceived;
        
        /// <summary>
        /// 訂閱特定股票代碼的即時成交數據 (Ticks)。
        /// </summary>
        /// <param name="symbol">要訂閱的股票代碼 (例如: "2330")。</param>
        void Subscribe(string symbol);
    }
}
