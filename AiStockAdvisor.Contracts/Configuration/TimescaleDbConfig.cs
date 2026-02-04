namespace AiStockAdvisor.Contracts.Configuration
{
    /// <summary>
    /// TimescaleDB 連線設定
    /// </summary>
    public class TimescaleDbConfig
    {
        public string Host { get; set; } = "192.168.0.43";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "stock_data";
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 產生連線字串
        /// </summary>
        public string ConnectionString =>
            $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
    }
}
