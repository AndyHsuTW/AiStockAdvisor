using System.Collections.Generic;
using YuantaOneAPI;

namespace AiStockAdvisor.Infrastructure.Yuanta
{
    /// <summary>
    /// 定義元大 API Wrapper 的適配器介面。
    /// 透過抽象化第三方 DLL，支援依賴注入與單元測試。
    /// </summary>
    public interface IYuantaApiAdapter : System.IDisposable
    {
        /// <summary>
        /// 當收到元大 API 伺服器回應時觸發的事件。
        /// </summary>
        event OnResponseEventHandler OnResponse;

        /// <summary>
        /// 設定是否顯示 API 的彈出視窗訊息。
        /// </summary>
        /// <param name="isPop">若設為 <c>true</c>，則啟用彈出訊息；否則停用。</param>
        void SetPopUpMsg(bool isPop);

        /// <summary>
        /// 設定 API 記錄 log 的方式。
        /// </summary>
        /// <param name="logType">Log 類型。</param>
        void SetLogType(enumLogType logType);

        /// <summary>
        /// 啟動連線並登入元大 API 環境。
        /// </summary>
        /// <param name="env">目標環境 (例如：UAT 或 Production)。</param>
        void Open(enumEnvironmentMode env);

        /// <summary>
        /// 執行帳號登入。
        /// </summary>
        /// <param name="userID">使用者帳號。</param>
        /// <param name="password">使用者密碼。</param>
        /// <returns>登入指令是否發送成功。</returns>
        bool Login(string userID, string password);

        /// <summary>
        /// 訂閱股票清單的即時成交數據 (Ticks)。
        /// </summary>
        /// <param name="reqList">包含欲訂閱股票的 <see cref="StockTick"/> 物件清單。</param>
        void SubscribeStockTick(List<StockTick> reqList);

        /// <summary>
        /// 關閉與 API 的連線。
        /// </summary>
        void Close();
    }
}
