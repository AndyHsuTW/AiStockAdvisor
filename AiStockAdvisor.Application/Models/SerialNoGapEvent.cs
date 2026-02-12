using System;

namespace AiStockAdvisor.Application.Models
{
    /// <summary>
    /// 代表單一股票發生 SerialNo 缺號事件。
    /// </summary>
    public sealed class SerialNoGapEvent
    {
        /// <summary>
        /// 初始化 <see cref="SerialNoGapEvent"/> 類別的新執行個體。
        /// </summary>
        /// <param name="stockCode">股票代碼。</param>
        /// <param name="previousSerialNo">前一筆序號。</param>
        /// <param name="currentSerialNo">目前序號。</param>
        /// <param name="missingStartSerialNo">缺號起始序號。</param>
        /// <param name="missingEndSerialNo">缺號結束序號。</param>
        /// <param name="tickTime">觸發缺號的 Tick 時間。</param>
        /// <param name="detectedAt">系統偵測時間。</param>
        public SerialNoGapEvent(
            string stockCode,
            int previousSerialNo,
            int currentSerialNo,
            int missingStartSerialNo,
            int missingEndSerialNo,
            DateTime tickTime,
            DateTime detectedAt)
        {
            StockCode = stockCode;
            PreviousSerialNo = previousSerialNo;
            CurrentSerialNo = currentSerialNo;
            MissingStartSerialNo = missingStartSerialNo;
            MissingEndSerialNo = missingEndSerialNo;
            TickTime = tickTime;
            DetectedAt = detectedAt;
        }

        /// <summary>
        /// 取得股票代碼。
        /// </summary>
        public string StockCode { get; }

        /// <summary>
        /// 取得前一筆序號。
        /// </summary>
        public int PreviousSerialNo { get; }

        /// <summary>
        /// 取得目前序號。
        /// </summary>
        public int CurrentSerialNo { get; }

        /// <summary>
        /// 取得缺號起始序號。
        /// </summary>
        public int MissingStartSerialNo { get; }

        /// <summary>
        /// 取得缺號結束序號。
        /// </summary>
        public int MissingEndSerialNo { get; }

        /// <summary>
        /// 取得缺號筆數。
        /// </summary>
        public int MissingCount => MissingEndSerialNo - MissingStartSerialNo + 1;

        /// <summary>
        /// 取得觸發缺號的 Tick 時間。
        /// </summary>
        public DateTime TickTime { get; }

        /// <summary>
        /// 取得系統偵測時間。
        /// </summary>
        public DateTime DetectedAt { get; }
    }
}
