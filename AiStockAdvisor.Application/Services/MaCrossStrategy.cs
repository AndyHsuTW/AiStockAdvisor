using System;
using System.Collections.Generic;
using System.Linq;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Interfaces;

namespace AiStockAdvisor.Application.Services
{
    /// <summary>
    /// 簡單的移動平均線交叉策略範例。
    /// 當短期 MA 向上突破長期 MA 時買入 (模擬信號)。
    /// </summary>
    public class MaCrossStrategy : ITradingStrategy
    {
        private readonly int _shortPeriod;
        private readonly int _longPeriod;
        private readonly List<decimal> _closePrices = new List<decimal>();
        private readonly ILogger _logger;

        public string Name => "MA Cross Strategy";

        public MaCrossStrategy(ILogger logger, int shortPeriod = 5, int longPeriod = 10)
        {
            _logger = logger;
            _shortPeriod = shortPeriod;
            _longPeriod = longPeriod;
        }

        public void OnTick(Tick tick)
        {
            // Do nothing on tick for now
        }

        public void OnBar(KBar bar)
        {
            _logger.LogInformation($"[{Name}] Received Bar: {bar}");
            
            _closePrices.Add(bar.Close);

            // Simple MA calculation
            if (_closePrices.Count >= _longPeriod)
            {
                var shortMa = CalculateMa(_shortPeriod);
                var longMa = CalculateMa(_longPeriod);
                
                _logger.LogInformation($"[{Name}] MA{_shortPeriod}: {shortMa:F2}, MA{_longPeriod}: {longMa:F2}");

                // Logic: Golden Cross logic could go here
                if (shortMa > longMa)
                {
                    _logger.LogInformation($"[{Name}] Signal: BULLISH (MA{_shortPeriod} > MA{_longPeriod})");
                }
                else
                {
                    _logger.LogInformation($"[{Name}] Signal: BEARISH (MA{_shortPeriod} < MA{_longPeriod})");
                }
            }
        }

        private decimal CalculateMa(int period)
        {
            return _closePrices.Skip(_closePrices.Count - period).Take(period).Average();
        }
    }
}
