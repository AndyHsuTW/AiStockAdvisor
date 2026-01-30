using System;
using System.Threading;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Infrastructure.Yuanta;
using AiStockAdvisor.Infrastructure.Logging;
using AiStockAdvisor.Infrastructure.Messaging;
using AiStockAdvisor.Infrastructure.Configuration;
using AiStockAdvisor.Application.Services;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Logging;

namespace AiStockAdvisor.ConsoleUI
{
    /// <summary>
    /// AI 證券交易顧問應用程式的進入點。
    /// 展示元大 Broker Client 的手動使用方式。
    /// </summary>
    class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        /// <param name="args">命令列參數 (未使用)。</param>
        [STAThread]
        static void Main(string[] args)
        {
            // 從 .env 檔案載入環境變數
            var envCount = DotEnvLoader.Load();
            if (envCount > 0)
            {
                Console.WriteLine($"Loaded {envCount} variables from .env file.");
            }

            Console.WriteLine("Starting AiStockAdvisor...");

            try
            {
                using var _ = LogScope.BeginFlow();

                // Dependency Injection Setup
                // Using 'using' block or explicit Dispose to ensure logs are flushed
                using (var logger = new FileLogger()) 
                {
                    IBrokerClient broker = new YuantaBrokerClient(logger);
                    TradingOrchestrator orchestrator = new TradingOrchestrator(broker, logger);

                    // 建立 RabbitMQ Publisher (從環境變數讀取設定，或使用預設值)
                    // 設定環境變數 RABBITMQ_ENABLED=false 可禁用發布
                    var tickPublisher = TickPublisherFactory.Create(logger: logger);
                    
                    // 訂閱 Tick 事件並發布到 RabbitMQ
                    broker.OnTickReceived += tick => tickPublisher.Publish(tick);

                    // Register Strategies
                    orchestrator.RegisterStrategy(new MaCrossStrategy(logger, shortPeriod: 2, longPeriod: 5));

                    // Start
                    string symbol = "2327"; // YAGEO (Updated by user)
                    
                    Console.Write("Enter Yuanta Account (ID): ");
                    string username = Console.ReadLine();
                    
                    Console.Write("Enter Password: ");
                    // Simple password reading (masking skipped for simplicity in console demo)
                    string password = Console.ReadLine();

                    orchestrator.Start(symbol, username, password);
                    
                    Console.WriteLine("System running. Press 'Q' to quit.");

                    // Keep alive while pumping WinForms messages (important for COM/ActiveX callbacks).
                    while (true)
                    {
                        System.Windows.Forms.Application.DoEvents();

                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Q)
                            {
                                tickPublisher.Close();
                                (broker as IDisposable)?.Dispose();
                                break;
                            }
                        }

                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
