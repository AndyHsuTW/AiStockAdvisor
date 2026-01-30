using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Infrastructure.Messaging
{
    /// <summary>
    /// 空實作的 ITickPublisher，用於禁用發布功能或測試。
    /// </summary>
    public class NullTickPublisher : Application.Interfaces.ITickPublisher
    {
        /// <inheritdoc />
        public void Publish(Tick tick)
        {
            // 不執行任何操作
        }

        /// <inheritdoc />
        public void Publish(Tick tick, int buyPriceRaw, int sellPriceRaw, int inOutFlag, int tickType)
        {
            // 不執行任何操作
        }

        /// <inheritdoc />
        public void Close()
        {
            // 不執行任何操作
        }
    }
}
