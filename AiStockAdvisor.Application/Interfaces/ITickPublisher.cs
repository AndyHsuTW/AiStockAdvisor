using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Application.Interfaces
{
    /// <summary>
    /// 定義股票 Tick 資料發布到訊息佇列的契約。
    /// </summary>
    public interface ITickPublisher
    {
        /// <summary>
        /// 發布 Tick 資料到訊息佇列。
        /// 使用預設的買賣價和內外盤註記（皆為 0）。
        /// </summary>
        /// <param name="tick">要發布的 Tick 資料。</param>
        void Publish(Tick tick);

        /// <summary>
        /// 發布 Tick 資料到訊息佇列，包含完整的原始欄位。
        /// </summary>
        /// <param name="tick">要發布的 Tick 資料。</param>
        /// <param name="buyPriceRaw">買價原始值。</param>
        /// <param name="sellPriceRaw">賣價原始值。</param>
        /// <param name="inOutFlag">內外盤註記。</param>
        /// <param name="tickType">明細類別。</param>
        void Publish(Tick tick, int buyPriceRaw, int sellPriceRaw, int inOutFlag, int tickType);

        /// <summary>
        /// 關閉與訊息佇列的連線。
        /// </summary>
        void Close();
    }
}
