using AiStockAdvisor.Application.Models;

namespace AiStockAdvisor.Application.Interfaces
{
    /// <summary>
    /// 定義 SerialNo 缺號通知服務契約。
    /// </summary>
    public interface IGapAlertService
    {
        /// <summary>
        /// 發送 SerialNo 缺號通知。
        /// </summary>
        /// <param name="gapEvent">缺號事件資料。</param>
        void NotifySerialNoGap(SerialNoGapEvent gapEvent);
    }
}
