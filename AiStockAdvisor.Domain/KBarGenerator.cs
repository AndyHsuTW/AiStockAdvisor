using System;

namespace AiStockAdvisor.Domain
{
    /// <summary>
    /// 負責將即時 Tick 資料聚合為 K 線 (KBar) 的領域服務。
    /// 目前支援固定 1 分鐘 K 線。
    /// </summary>
    public class KBarGenerator
    {
        private KBar _currentBar;
        private readonly TimeSpan _period;

        /// <summary>
        /// 當 K 線完成 (Close) 時觸發的事件。
        /// </summary>
        public event Action<KBar> OnBarClosed;

        /// <summary>
        /// 初始化 <see cref="KBarGenerator"/> 類別的新執行個體。
        /// </summary>
        /// <param name="period">K 線週期 (預設 1 分鐘)。</param>
        public KBarGenerator(TimeSpan period)
        {
            _period = period;
        }

        /// <summary>
        /// 接收新的 Tick 資料並更新當前 K 線狀態。
        /// </summary>
        /// <param name="tick">最新的 Tick 成交資訊。</param>
        public void Update(Tick tick)
        {
            if (tick == null) return;

            // 計算該 Tick 所屬的 K 線時間 (例如 09:00:05 -> 09:01:00)
            // 這裡採用簡單的整點切分邏輯
            long ticks = tick.Time.Ticks;
            long periodTicks = _period.Ticks;
            long remainder = ticks % periodTicks;
            
            // 下一根 Bar 的結算時間 (開盤時間 + 週期)
            // Example: 09:00:05 for 1min bar -> Start 09:00:00 -> End 09:01:00
            DateTime barEndTime = new DateTime(ticks - remainder + periodTicks);

            if (_currentBar == null)
            {
                CreateNewBar(barEndTime, tick);
            }
            else
            {
                if (barEndTime > _currentBar.Time)
                {
                    // 舊的 Bar 結束，發送事件
                    OnBarClosed?.Invoke(_currentBar);
                    
                    // 開啟新的 Bar
                    CreateNewBar(barEndTime, tick);
                }
                else
                {
                    // 更新當前 Bar
                    UpdateCurrentBar(tick);
                }
            }
        }

        private void CreateNewBar(DateTime endTime, Tick tick)
        {
            // Open, High, Low, Close, Volume
            _currentBar = new KBar(endTime, tick.Price, tick.Price, tick.Price, tick.Price, tick.Volume);
        }

        private void UpdateCurrentBar(Tick tick)
        {
            decimal newHigh = Math.Max(_currentBar.High, tick.Price);
            decimal newLow = Math.Min(_currentBar.Low, tick.Price);
            decimal newClose = tick.Price;
            decimal newVolume = _currentBar.Volume + tick.Volume;

            // KBar is immutable, so create a new instance (Value Object pattern)
            // Or technically KBar should be mutable during formation? 
            // For safety, treating it as immutable value object replacement.
            _currentBar = new KBar(_currentBar.Time, _currentBar.Open, newHigh, newLow, newClose, newVolume);
        }
    }
}
