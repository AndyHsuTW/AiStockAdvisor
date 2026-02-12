using System;
using System.Collections.Concurrent;
using AiStockAdvisor.Application.Models;
using AiStockAdvisor.Domain;

namespace AiStockAdvisor.Application.Services
{
    /// <summary>
    /// 偵測同一股票序列中的 SerialNo 缺號。
    /// </summary>
    public sealed class TickGapDetector
    {
        /// <summary>
        /// 每檔股票的序號追蹤狀態。
        /// </summary>
        private sealed class StockGapState
        {
            /// <summary>
            /// 取得或設定目前最後序號。
            /// </summary>
            public int LastSerialNo { get; set; }

            /// <summary>
            /// 取得或設定是否已有第一筆有效序號。
            /// </summary>
            public bool HasLastSerialNo { get; set; }

            /// <summary>
            /// 取得狀態鎖。
            /// </summary>
            public object SyncRoot { get; } = new object();
        }

        private readonly ConcurrentDictionary<string, StockGapState> _states
            = new ConcurrentDictionary<string, StockGapState>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 嘗試偵測 Tick 是否出現 SerialNo 缺號。
        /// </summary>
        /// <param name="tick">逐筆成交資料。</param>
        /// <param name="gapEvent">缺號事件資料；若未缺號則為 null。</param>
        /// <returns>若偵測到缺號則為 true。</returns>
        public bool TryDetectGap(Tick tick, out SerialNoGapEvent? gapEvent)
        {
            if (tick == null) throw new ArgumentNullException(nameof(tick));

            gapEvent = null;

            var stockCode = NormalizeStockCode(tick.Symbol);
            if (string.IsNullOrEmpty(stockCode))
            {
                return false;
            }

            // 0 或負值視為無效序號，不納入缺號判斷。
            if (tick.SerialNo <= 0)
            {
                return false;
            }

            var state = _states.GetOrAdd(stockCode, _ => new StockGapState());
            lock (state.SyncRoot)
            {
                if (!state.HasLastSerialNo)
                {
                    state.LastSerialNo = tick.SerialNo;
                    state.HasLastSerialNo = true;
                    return false;
                }

                if (tick.SerialNo > state.LastSerialNo + 1)
                {
                    gapEvent = new SerialNoGapEvent(
                        stockCode: stockCode,
                        previousSerialNo: state.LastSerialNo,
                        currentSerialNo: tick.SerialNo,
                        missingStartSerialNo: state.LastSerialNo + 1,
                        missingEndSerialNo: tick.SerialNo - 1,
                        tickTime: tick.Time,
                        detectedAt: DateTime.Now);

                    state.LastSerialNo = tick.SerialNo;
                    return true;
                }

                if (tick.SerialNo > state.LastSerialNo)
                {
                    state.LastSerialNo = tick.SerialNo;
                }

                return false;
            }
        }

        /// <summary>
        /// 將股票代碼正規化為可比較格式。
        /// </summary>
        /// <param name="stockCode">原始股票代碼。</param>
        /// <returns>正規化後股票代碼；若無效則回傳空字串。</returns>
        private static string NormalizeStockCode(string? stockCode)
        {
            return stockCode?.Trim() ?? string.Empty;
        }
    }
}
