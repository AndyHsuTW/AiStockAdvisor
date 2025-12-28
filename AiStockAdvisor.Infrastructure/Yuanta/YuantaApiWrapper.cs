using System.Collections.Generic;
using YuantaOneAPI;

namespace AiStockAdvisor.Infrastructure.Yuanta
{
    /// <summary>
    /// <see cref="IYuantaApiAdapter"/> 的實作類別。
    /// 將呼叫委派給實際的 <see cref="YuantaOneAPITrader"/> 類別。
    /// </summary>
    public class YuantaApiWrapper : IYuantaApiAdapter
    {
        private readonly YuantaOneAPITrader _trader;

        /// <inheritdoc />
        public event OnResponseEventHandler OnResponse
        {
            add => _trader.OnResponse += value;
            remove => _trader.OnResponse -= value;
        }

        /// <summary>
        /// 初始化 <see cref="YuantaApiWrapper"/> 類別的新執行個體。
        /// 實例化底層的 <see cref="YuantaOneAPITrader"/>。
        /// </summary>
        public YuantaApiWrapper()
        {
            _trader = new YuantaOneAPITrader();
        }

        /// <inheritdoc />
        public void SetPopUpMsg(bool isPop)
        {
            _trader.SetPopUpMsg(isPop);
        }

        /// <inheritdoc />
        public void SetLogType(enumLogType logType)
        {
            _trader.SetLogType(logType);
        }

        /// <inheritdoc />
        public void Open(enumEnvironmentMode env)
        {
            _trader.Open(env);
        }

        /// <inheritdoc />
        public bool Login(string userID, string password)
        {
            return _trader.Login(userID, password);
        }

        /// <inheritdoc />
        public void SubscribeStockTick(List<StockTick> reqList)
        {
            _trader.SubscribeStockTick(reqList);
        }

        /// <inheritdoc />
        public void Close()
        {
            _trader.Close();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _trader.Dispose();
        }
    }
}
