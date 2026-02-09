using System;
using System.Linq;
using AiStockAdvisor.Infrastructure.Configuration;
using Xunit;

namespace AiStockAdvisor.Tests.Infrastructure.Configuration
{
    /// <summary>
    /// 驗證股票代碼環境變數解析行為。
    /// </summary>
    public class StockConfigTests
    {
        private const string EnvKey = "STOCK_SYMBOLS";

        /// <summary>
        /// 未設定環境變數時應回傳預設股票。
        /// </summary>
        [Fact]
        public void GetSymbols_WhenEnvMissing_ShouldReturnDefault()
        {
            WithEnv(null, () =>
            {
                var symbols = StockConfig.GetSymbols();

                Assert.Single(symbols);
                Assert.Equal("2327", symbols[0]);
            });
        }

        /// <summary>
        /// 應去除空白與空 token，且保留第一個有效順序。
        /// </summary>
        [Fact]
        public void GetSymbols_ShouldTrimRemoveEmptyAndDistinct()
        {
            WithEnv(" 2327 , ,2330, 2327 ,2454, ", () =>
            {
                var symbols = StockConfig.GetSymbols();

                Assert.Equal(new[] { "2327", "2330", "2454" }, symbols);
            });
        }

        /// <summary>
        /// 應保留首次出現順序，後續重複項目略過。
        /// </summary>
        [Fact]
        public void GetSymbols_ShouldPreserveFirstAppearanceOrder()
        {
            WithEnv("2454,2330,2454,2327,2330", () =>
            {
                var symbols = StockConfig.GetSymbols();

                Assert.Equal(new[] { "2454", "2330", "2327" }, symbols);
            });
        }

        /// <summary>
        /// 全部 token 都無效時應回傳預設股票。
        /// </summary>
        [Fact]
        public void GetSymbols_WhenAllTokensInvalid_ShouldReturnDefault()
        {
            WithEnv(" ,  ,", () =>
            {
                var symbols = StockConfig.GetSymbols();

                Assert.True(symbols.SequenceEqual(new[] { "2327" }));
            });
        }

        /// <summary>
        /// 在測試期間暫時覆寫環境變數，結束後還原原值。
        /// </summary>
        /// <param name="value">測試期間要設定的值；null 代表移除。</param>
        /// <param name="action">在指定環境變數下執行的測試動作。</param>
        private static void WithEnv(string? value, Action action)
        {
            var original = Environment.GetEnvironmentVariable(EnvKey);
            try
            {
                Environment.SetEnvironmentVariable(EnvKey, value);
                action();
            }
            finally
            {
                Environment.SetEnvironmentVariable(EnvKey, original);
            }
        }
    }
}
