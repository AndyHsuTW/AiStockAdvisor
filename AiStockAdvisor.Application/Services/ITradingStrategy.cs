using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Application.Services
{
    /// <summary>
    /// 定義交易策略的通用介面。
    /// </summary>
    public interface ITradingStrategy
    {
        /// <summary>
        /// 策略名稱。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 當接收到新的 K 線時觸發。
        /// </summary>
        /// <param name="bar">最新的 K 線資料。</param>
        void OnBar(KBar bar);

        /// <summary>
        /// 當接收到新的 Tick 時觸發 (選擇性實作)。
        /// </summary>
        /// <param name="tick">最新的 Tick 資料。</param>
        void OnTick(Tick tick);
    }
}
