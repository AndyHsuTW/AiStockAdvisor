using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AiStockAdvisor.Domain;
using AiStockAdvisor.Application.Interfaces;
using AiStockAdvisor.Logging;

namespace AiStockAdvisor.Application.Services
{
    /// <summary>
    /// 交易流程協調者 (Orchestrator)。
    /// 負責將 IBrokerClient 的數據串接至 KBarGenerator 與 ITradingStrategy。
    /// </summary>
    public class TradingOrchestrator
    {
        private readonly IBrokerClient _broker;
        private readonly ConcurrentDictionary<string, KBarGeneratorState> _kBarGenerators;
        private readonly TimeSpan _barPeriod;
        private readonly List<ITradingStrategy> _strategies;
        private readonly ILogger _logger;
        private string? _flowLogId;
        private string? _flowSpanId;

        public TradingOrchestrator(IBrokerClient broker, ILogger logger)
        {
            _broker = broker;
            _logger = logger;
            _barPeriod = TimeSpan.FromMinutes(1);
            _kBarGenerators = new ConcurrentDictionary<string, KBarGeneratorState>();
            _strategies = new List<ITradingStrategy>();

            // Wire up events
            _broker.OnTickReceived += HandleTick;
        }

        /// <summary>
        /// 單一股票 KBar 聚合所需的狀態與同步鎖。
        /// </summary>
        private sealed class KBarGeneratorState
        {
            /// <summary>
            /// 初始化 <see cref="KBarGeneratorState"/> 類別的新執行個體。
            /// </summary>
            /// <param name="generator">股票專屬的 KBar 產生器。</param>
            public KBarGeneratorState(KBarGenerator generator)
            {
                Generator = generator;
                SyncRoot = new object();
            }

            /// <summary>
            /// 取得股票專屬的 KBar 產生器。
            /// </summary>
            public KBarGenerator Generator { get; }

            /// <summary>
            /// 取得股票專屬更新鎖物件。
            /// </summary>
            public object SyncRoot { get; }
        }

        public void RegisterStrategy(ITradingStrategy strategy)
        {
            _strategies.Add(strategy);
            _logger.LogInformation(LogScope.FormatMessage($"[Orchestrator] Registered strategy: {strategy.Name}"));
        }

        /// <summary>
        /// 啟動交易流程，訂閱指定的多檔股票。
        /// </summary>
        /// <param name="symbols">股票代碼清單。</param>
        /// <param name="username">元大帳號。</param>
        /// <param name="password">密碼。</param>
        public void Start(string[] symbols, string username, string password)
        {
            _flowLogId = Guid.NewGuid().ToString();
            _flowSpanId = Guid.NewGuid().ToString();
            using (LogScope.Use(_flowLogId, _flowSpanId))
            {
                _logger.LogInformation(LogScope.FormatMessage(
                    $"[Orchestrator] Starting trading flow for {string.Join(", ", symbols)}..."));
                _broker.Login(username, password);

                foreach (var rawSymbol in symbols)
                {
                    if (string.IsNullOrWhiteSpace(rawSymbol))
                    {
                        _logger.LogWarning(LogScope.FormatMessage("[Orchestrator] Skipped empty symbol."));
                        continue;
                    }

                    var symbol = rawSymbol.Trim();
                    var gen = new KBarGenerator(symbol, _barPeriod);
                    gen.OnBarClosed += HandleBar;

                    if (!_kBarGenerators.TryAdd(symbol, new KBarGeneratorState(gen)))
                    {
                        _logger.LogWarning(LogScope.FormatMessage(
                            $"[Orchestrator] Skipped duplicate symbol: {symbol}"));
                        continue;
                    }

                    _broker.Subscribe(symbol);
                    _broker.SubscribeBest5(symbol);

                    _logger.LogInformation(LogScope.FormatMessage(
                        $"[Orchestrator] Subscribed to {symbol}"));
                }
            }
        }

        private void HandleTick(Tick tick)
        {
            using (LogScope.Use(_flowLogId, _flowSpanId))
            {
                // 依 tick.Symbol 路由到對應的 KBarGenerator
                var symbol = tick.Symbol?.Trim();
                if (symbol is { Length: > 0 } normalizedSymbol &&
                    _kBarGenerators.TryGetValue(normalizedSymbol, out var state))
                {
                    using (LogScope.BeginBranch())
                    {
                        lock (state.SyncRoot)
                        {
                            state.Generator.Update(tick);
                        }
                    }
                }

                // Pass to strategies if they need ticks
                foreach (var strategy in _strategies)
                {
                    using (LogScope.BeginBranch())
                    {
                        strategy.OnTick(tick);
                    }
                }
            }
        }

        private void HandleBar(KBar bar)
        {
            using (LogScope.Use(_flowLogId, _flowSpanId))
            {
                // Pass closed bar to strategies
                foreach (var strategy in _strategies)
                {
                    using (LogScope.BeginBranch())
                    {
                        strategy.OnBar(bar);
                    }
                }
            }
        }
    }
}
