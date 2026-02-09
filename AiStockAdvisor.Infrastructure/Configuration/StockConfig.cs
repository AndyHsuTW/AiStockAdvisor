namespace AiStockAdvisor.Infrastructure.Configuration
{
    /// <summary>
    /// 股票訂閱設定，從環境變數 STOCK_SYMBOLS 讀取。
    /// </summary>
    public static class StockConfig
    {
        private const string EnvKey = "STOCK_SYMBOLS";
        private const string DefaultSymbol = "2327";

        /// <summary>
        /// 從環境變數解析股票代碼清單。
        /// 格式：逗號分隔，例如 "2327,2330,2454"。
        /// 若未設定，退回預設值 "2327"。
        /// </summary>
        public static string[] GetSymbols()
        {
            var raw = System.Environment.GetEnvironmentVariable(EnvKey);
            if (string.IsNullOrWhiteSpace(raw))
                return new[] { DefaultSymbol };

            var symbols = raw.Split(',');
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var result = new System.Collections.Generic.List<string>();
            foreach (var s in symbols)
            {
                var trimmed = s.Trim();
                if (!string.IsNullOrEmpty(trimmed) && seen.Add(trimmed))
                    result.Add(trimmed);
            }

            return result.Count > 0 ? result.ToArray() : new[] { DefaultSymbol };
        }
    }
}
