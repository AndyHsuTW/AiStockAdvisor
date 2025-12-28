using System;
using System.Collections.Generic;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Interfaces;

namespace AiStockAdvisor.Application.Services
{
    /// <summary>
    /// 交易流程協調者 (Orchestrator)。
    /// 負責將 IBrokerClient 的數據串接至 KBarGenerator 與 ITradingStrategy。
    /// </summary>
    public class TradingOrchestrator
    {
        private readonly IBrokerClient _broker;
        private readonly KBarGenerator _kBarGenerator;
        private readonly List<ITradingStrategy> _strategies;
        private readonly ILogger _logger;

        public TradingOrchestrator(IBrokerClient broker, ILogger logger)
        {
            _broker = broker;
            _logger = logger;
            _kBarGenerator = new KBarGenerator(TimeSpan.FromMinutes(1)); // Default 1 min
            _strategies = new List<ITradingStrategy>();

            // Wire up events
            _broker.OnTickReceived += HandleTick;
            _kBarGenerator.OnBarClosed += HandleBar;
        }

        public void RegisterStrategy(ITradingStrategy strategy)
        {
            _strategies.Add(strategy);
            _logger.LogInformation($"[Orchestrator] Registered strategy: {strategy.Name}");
        }

        public void Start(string symbol, string username, string password)
        {
            _logger.LogInformation($"[Orchestrator] Starting trading flow for {symbol}...");
            _broker.Login(username, password);
            
            // Wait for login? In real app, need event. Here assuming synchronous enough or retry.
            // But Broker.Login() is async in nature usually.
            // For now, simple call flow.
            
            _broker.Subscribe(symbol);
        }

        private void HandleTick(Tick tick)
        {
            // Update KBar Generator
            _kBarGenerator.Update(tick);

            // Pass to strategies if they need ticks
            foreach (var strategy in _strategies)
            {
                strategy.OnTick(tick);
            }
        }

        private void HandleBar(KBar bar)
        {
            // Pass closed bar to strategies
            foreach (var strategy in _strategies)
            {
                strategy.OnBar(bar);
            }
        }
    }
}
