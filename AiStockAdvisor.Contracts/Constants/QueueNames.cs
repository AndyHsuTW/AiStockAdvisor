namespace AiStockAdvisor.Contracts.Constants
{
    public static class QueueNames
    {
        /// <summary>TradingCore 消費 Tick 的 Queue</summary>
        public const string TradingCoreTicks = "trading-core.ticks";

        /// <summary>DbWriter 消費 Tick 的 Queue</summary>
        public const string DbWriterTicks = "db-writer.ticks";
    }
}
