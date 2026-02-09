using System;
using System.Collections.Generic;
using System.Linq;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Logging;

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
        private readonly Dictionary<string, List<decimal>> _closePricesBySymbol
            = new Dictionary<string, List<decimal>>();
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
            var symbol = bar.Symbol;
            _logger.LogInformation(LogScope.FormatMessage($"[{Name}][{symbol}] Received Bar: {bar}"));

            if (!_closePricesBySymbol.TryGetValue(symbol, out var closePrices))
            {
                closePrices = new List<decimal>();
                _closePricesBySymbol[symbol] = closePrices;
            }

            closePrices.Add(bar.Close);

            // Simple MA calculation
            if (closePrices.Count >= _longPeriod)
            {
                var shortMa = CalculateMa(closePrices, _shortPeriod);
                var longMa = CalculateMa(closePrices, _longPeriod);
                
                _logger.LogInformation(LogScope.FormatMessage(
                    $"[{Name}][{symbol}] MA{_shortPeriod}: {shortMa:F2}, MA{_longPeriod}: {longMa:F2}"));

                // Logic: Golden Cross logic could go here
                if (shortMa > longMa)
                {
                    _logger.LogInformation(LogScope.FormatMessage(
                        $"[{Name}][{symbol}] Signal: BULLISH (MA{_shortPeriod} > MA{_longPeriod})"));
                }
                else
                {
                    _logger.LogInformation(LogScope.FormatMessage(
                        $"[{Name}][{symbol}] Signal: BEARISH (MA{_shortPeriod} < MA{_longPeriod})"));
                }
            }
        }

        private decimal CalculateMa(List<decimal> prices, int period)
        {
            return prices.Skip(prices.Count - period).Take(period).Average();
        }
    }
}
