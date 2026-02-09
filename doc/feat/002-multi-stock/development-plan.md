# 002 — 多檔股票訂閱功能開發計劃

> **目標**：將系統從只能訂閱單一股票 (`2327`) 擴展為可同時訂閱多檔股票，  
> 並確保 Tick 處理、K 線聚合、策略運算、RabbitMQ 發布等環節正確隔離各股票資料。

---

## 一、現況分析

### 1.1 目前資料流

```
Program.Main()
  ├─ symbol = "2327"  (硬編碼)
  ├─ orchestrator.Start(symbol, ...)
  │     ├─ broker.Subscribe(symbol)       ← 只訂閱一檔
  │     └─ broker.SubscribeBest5(symbol)  ← 只訂閱一檔
  │
  └─ 事件驅動：
       broker.OnTickReceived
         ├── TradingOrchestrator.HandleTick()
         │     ├─ KBarGenerator.Update(tick)  ← 單一實例，不分股票
         │     └─ strategy.OnTick(tick)       ← 策略不分股票
         └── tickPublisher.Publish(tick)      ← RoutingKey 固定
```

### 1.2 瓶頸清單

| # | 元件 | 檔案 | 問題 |
|---|------|------|------|
| 1 | `Program.cs` | `ConsoleUI/Program.cs` L58 | `symbol = "2327"` 硬編碼 |
| 2 | `TradingOrchestrator.Start()` | `Application/Services/TradingOrchestrator.cs` L43 | 只接受 `string symbol` 單一檔 |
| 3 | `KBarGenerator` | `Domain/KBarGenerator.cs` | 全域單一實例，不區分股票 |
| 4 | `KBar` | `Domain/KBar.cs` | 無 `Symbol` 屬性，無法辨別歸屬 |
| 5 | `ITradingStrategy` | `Application/Services/ITradingStrategy.cs` | `OnBar(KBar)` / `OnTick(Tick)` 不帶 symbol 上下文 |
| 6 | `MaCrossStrategy` | `Application/Services/MaCrossStrategy.cs` | `_closePrices` 為單一 List，多股混入會算錯 |
| 7 | `RabbitMQ RoutingKey` | `Infrastructure/Messaging/RabbitMqConfig.cs` | 固定 `stock.twse.tick` |
| 8 | `.env` | 根目錄 | 無股票清單設定項 |

### 1.3 已有多股友善設計（無需大改）

| 元件 | 說明 |
|------|------|
| `YuantaBrokerClient.Subscribe()` | 底層 API `SubscribeStockTick(List<StockTick>)` 原生支援批次訂閱 |
| `YuantaBrokerClient._lastTickBySymbol` | 已用 `Dictionary<string, TickSignature>` 按 symbol 去重 |
| `Tick.Symbol` | Domain Tick 已有 `Symbol` 屬性 |
| `TickMessage.StockCode` | Contracts TickMessage 已有 `StockCode` 欄位 |

---

## 二、設計方案

### 2.1 設計原則

1. **向下相容**：若只設定一檔股票，行為與現有完全一致
2. **最小改動面**：不改 `IBrokerClient` 介面簽章，在 Orchestrator 層迴圈呼叫
3. **每股隔離**：KBar 聚合與策略運算按股票隔離，避免資料交叉污染
4. **可設定**：股票清單從 `.env` 環境變數讀取，方便部署調整

### 2.2 架構概覽（修改後）

```
Program.Main()
  ├─ symbols = ["2327", "2330", "2454"]   ← 從 .env 讀取
  ├─ orchestrator.Start(symbols, ...)
  │     ├─ foreach symbol → broker.Subscribe(symbol)
  │     └─ foreach symbol → broker.SubscribeBest5(symbol)
  │
  └─ 事件驅動：
       broker.OnTickReceived(tick)
         ├── TradingOrchestrator.HandleTick(tick)
         │     ├─ _kBarGenerators[tick.Symbol].Update(tick)   ← 按股票分流
         │     └─ strategy.OnTick(tick)                       ← Tick 已帶 Symbol
         └── tickPublisher.Publish(tick)                      ← RoutingKey 含 symbol
```

---

## 三、分步實施計劃

### Phase 1：設定層 — 從 `.env` 讀取多檔股票清單

#### Task 1-1：新增 `.env` 設定項

在 `.env` 中新增：

```env
# 訂閱股票清單（逗號分隔）
STOCK_SYMBOLS=2327,2330,2454
```

#### Task 1-2：建立 `StockConfig` 設定解析

**新增檔案**：`AiStockAdvisor.Infrastructure/Configuration/StockConfig.cs`

```csharp
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
            var result = new System.Collections.Generic.List<string>();
            foreach (var s in symbols)
            {
                var trimmed = s.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }

            return result.Count > 0 ? result.ToArray() : new[] { DefaultSymbol };
        }
    }
}
```

#### Task 1-3：更新 `Program.cs`

```diff
- string symbol = "2327"; // YAGEO (Updated by user)
+ string[] symbols = StockConfig.GetSymbols();
+ Console.WriteLine($"Subscribing to: {string.Join(", ", symbols)}");
```

**驗收標準**：  
- `STOCK_SYMBOLS` 未設定 → 預設 `["2327"]`，行為不變  
- `STOCK_SYMBOLS=2327,2330` → 解析為 `["2327", "2330"]`

---

### Phase 2：Domain 層 — 為 KBar 加入 Symbol

#### Task 2-1：`KBar` 新增 `Symbol` 屬性

**修改檔案**：`AiStockAdvisor.Domain/KBar.cs`

```diff
  public class KBar
  {
+     /// <summary>
+     /// 取得此 K 線所屬的股票代碼。
+     /// </summary>
+     public string Symbol { get; }

      public DateTime Time { get; }
      public decimal Open { get; }
      // ... 其他屬性不變

-     public KBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
+     public KBar(string symbol, DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
      {
+         Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
          if (high < low)
              throw new ArgumentException("High price cannot be lower than Low price.");
          Time = time;
          // ...
      }
  }
```

#### Task 2-2：`KBarGenerator` 改為接收 symbol

**修改檔案**：`AiStockAdvisor.Domain/KBarGenerator.cs`

新增 `_symbol` 欄位，在建構時傳入，用於 `CreateNewBar()` 時帶入 `KBar`。

```diff
  public class KBarGenerator
  {
      private KBar _currentBar;
      private readonly TimeSpan _period;
+     private readonly string _symbol;

-     public KBarGenerator(TimeSpan period)
+     public KBarGenerator(string symbol, TimeSpan period)
      {
+         _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
          _period = period;
      }

      // ...

      private void CreateNewBar(DateTime endTime, Tick tick)
      {
-         _currentBar = new KBar(endTime, tick.Price, tick.Price, tick.Price, tick.Price, tick.Volume);
+         _currentBar = new KBar(_symbol, endTime, tick.Price, tick.Price, tick.Price, tick.Price, tick.Volume);
      }

      private void UpdateCurrentBar(Tick tick)
      {
          // ... 計算 newHigh, newLow, newClose, newVolume ...
-         _currentBar = new KBar(_currentBar.Time, _currentBar.Open, newHigh, newLow, newClose, newVolume);
+         _currentBar = new KBar(_symbol, _currentBar.Time, _currentBar.Open, newHigh, newLow, newClose, newVolume);
      }
  }
```

**驗收標準**：  
- `KBar` 的 `Symbol` 屬性不為 null  
- 既有的 `KBar` 單元測試需同步更新建構參數  

---

### Phase 3：Application 層 — Orchestrator 多股支援

#### Task 3-1：`TradingOrchestrator` 改為多股模式

**修改檔案**：`AiStockAdvisor.Application/Services/TradingOrchestrator.cs`

核心改動：將 `_kBarGenerator` 從單一實例改為 `Dictionary<string, KBarGenerator>`。

```diff
  public class TradingOrchestrator
  {
      private readonly IBrokerClient _broker;
-     private readonly KBarGenerator _kBarGenerator;
+     private readonly Dictionary<string, KBarGenerator> _kBarGenerators;
+     private readonly TimeSpan _barPeriod;
      private readonly List<ITradingStrategy> _strategies;
      private readonly ILogger _logger;

      public TradingOrchestrator(IBrokerClient broker, ILogger logger)
      {
          _broker = broker;
          _logger = logger;
-         _kBarGenerator = new KBarGenerator(TimeSpan.FromMinutes(1));
+         _barPeriod = TimeSpan.FromMinutes(1);
+         _kBarGenerators = new Dictionary<string, KBarGenerator>();
          _strategies = new List<ITradingStrategy>();

          _broker.OnTickReceived += HandleTick;
-         _kBarGenerator.OnBarClosed += HandleBar;
      }

-     public void Start(string symbol, string username, string password)
+     public void Start(string[] symbols, string username, string password)
      {
          _logger.LogInformation(LogScope.FormatMessage(
-             $"[Orchestrator] Starting trading flow for {symbol}..."));
+             $"[Orchestrator] Starting trading flow for {string.Join(", ", symbols)}..."));
          _broker.Login(username, password);

-         _broker.Subscribe(symbol);
-         _broker.SubscribeBest5(symbol);
+         foreach (var symbol in symbols)
+         {
+             // 為每檔股票建立獨立 KBarGenerator
+             var gen = new KBarGenerator(symbol, _barPeriod);
+             gen.OnBarClosed += HandleBar;
+             _kBarGenerators[symbol] = gen;
+
+             _broker.Subscribe(symbol);
+             _broker.SubscribeBest5(symbol);
+
+             _logger.LogInformation(LogScope.FormatMessage(
+                 $"[Orchestrator] Subscribed to {symbol}"));
+         }
      }

      private void HandleTick(Tick tick)
      {
-         // Update KBar Generator
-         _kBarGenerator.Update(tick);
+         // 依 tick.Symbol 路由到對應的 KBarGenerator
+         var symbol = tick.Symbol?.Trim();
+         if (symbol != null && _kBarGenerators.TryGetValue(symbol, out var gen))
+         {
+             gen.Update(tick);
+         }

          foreach (var strategy in _strategies)
          {
              strategy.OnTick(tick);
          }
      }
  }
```

#### Task 3-2：`Program.cs` 呼叫端更新

```diff
- orchestrator.Start(symbol, username, password);
+ orchestrator.Start(symbols, username, password);
```

**驗收標準**：  
- 訂閱 N 檔股票，系統建立 N 個獨立 `KBarGenerator`  
- 不同股票的 Tick 不會互相影響 K 線聚合  

---

### Phase 4：策略層 — 按股票隔離策略狀態

#### Task 4-1：`MaCrossStrategy` 改為按 symbol 隔離收盤價

**修改檔案**：`AiStockAdvisor.Application/Services/MaCrossStrategy.cs`

```diff
  public class MaCrossStrategy : ITradingStrategy
  {
-     private readonly List<decimal> _closePrices = new List<decimal>();
+     private readonly Dictionary<string, List<decimal>> _closePricesBySymbol
+         = new Dictionary<string, List<decimal>>();

      public void OnBar(KBar bar)
      {
-         _closePrices.Add(bar.Close);
-         if (_closePrices.Count >= _longPeriod)
+         var symbol = bar.Symbol;
+         if (!_closePricesBySymbol.TryGetValue(symbol, out var closePrices))
+         {
+             closePrices = new List<decimal>();
+             _closePricesBySymbol[symbol] = closePrices;
+         }
+
+         closePrices.Add(bar.Close);
+
+         if (closePrices.Count >= _longPeriod)
          {
-             var shortMa = CalculateMa(_shortPeriod);
-             var longMa = CalculateMa(_longPeriod);
+             var shortMa = CalculateMa(closePrices, _shortPeriod);
+             var longMa = CalculateMa(closePrices, _longPeriod);
              // ... 日誌加入 symbol 以區分
+             _logger.LogInformation(LogScope.FormatMessage(
+                 $"[{Name}][{symbol}] MA{_shortPeriod}: {shortMa:F2}, MA{_longPeriod}: {longMa:F2}"));
          }
      }

-     private decimal CalculateMa(int period)
+     private decimal CalculateMa(List<decimal> prices, int period)
      {
-         return _closePrices.Skip(_closePrices.Count - period).Take(period).Average();
+         return prices.Skip(prices.Count - period).Take(period).Average();
      }
  }
```

**驗收標準**：  
- 股票 A 和 B 的 MA 各自獨立計算  
- 日誌中顯示對應的 symbol  

---

### Phase 5：RabbitMQ RoutingKey 動態化（可選增強）

#### Task 5-1：依 symbol 產生 RoutingKey

**修改檔案**：`AiStockAdvisor.Infrastructure/Messaging/RabbitMqTickPublisher.cs`

目前 RoutingKey 固定為 `stock.twse.tick`，改為依 Tick 動態產生：

```diff
  // 現有 Publish() 方法中：
- routingKey: _config.RoutingKey,
+ routingKey: $"stock.{tick.MarketNo}.tick.{tick.Symbol}",
```

這樣消費端可以使用 topic exchange 的萬用字元訂閱：
- `stock.1.tick.*` — 訂閱所有 TWSE 股票
- `stock.1.tick.2330` — 只訂閱台積電
- `stock.#` — 訂閱所有市場所有股票

> **注意**：此步驟需與下游消費者同步更新，建議先保留原有固定 RoutingKey 作為 fallback，
> 或先以 feature flag `RABBITMQ_DYNAMIC_ROUTING=true` 控制是否啟用。

**驗收標準**：  
- 動態 RoutingKey 對應到正確 symbol  
- 下游消費者使用萬用字元可正確接收  

---

### Phase 6：單元測試更新

#### Task 6-1：更新既有測試

受影響的測試檔案：

| 測試檔案 | 需修改原因 |
|---------|-----------|
| `AiStockAdvisor.Tests/Domain/KBarGeneratorTests.cs` | `KBarGenerator` 建構參數新增 `symbol` |
| `AiStockAdvisor.Tests/Domain/KBarTests.cs` (若存在) | `KBar` 建構參數新增 `symbol` |
| 其他使用 `new KBar(...)` 的測試 | 同上 |

#### Task 6-2：新增多股測試案例

**新增檔案**：`AiStockAdvisor.Tests/Domain/MultiStockKBarTests.cs`

測試情境：
1. 兩檔股票的 Tick 交互送入，各自產生正確的 KBar
2. KBar 的 `Symbol` 正確對應
3. 不同 symbol 的 tick 不會造成 bar 互相截斷

**新增檔案**：`AiStockAdvisor.Tests/Application/MultiStockOrchestratorTests.cs`

測試情境：
1. `Start(["2327", "2330"], ...)` 呼叫後，broker 被呼叫 Subscribe 兩次
2. 收到不同 symbol 的 tick，路由到正確的 KBarGenerator

**新增檔案**：`AiStockAdvisor.Tests/Application/MaCrossStrategyMultiStockTests.cs`

測試情境：
1. 分別餵入兩檔股票的 KBar，MA 各自計算不互相干擾

---

## 四、修改清單總覽

| # | 檔案 | 變更類型 | Phase |
|---|------|---------|-------|
| 1 | `.env` | 修改 | 1 |
| 2 | `Infrastructure/Configuration/StockConfig.cs` | **新增** | 1 |
| 3 | `ConsoleUI/Program.cs` | 修改 | 1, 3 |
| 4 | `Domain/KBar.cs` | 修改 | 2 |
| 5 | `Domain/KBarGenerator.cs` | 修改 | 2 |
| 6 | `Application/Services/TradingOrchestrator.cs` | 修改 | 3 |
| 7 | `Application/Services/MaCrossStrategy.cs` | 修改 | 4 |
| 8 | `Infrastructure/Messaging/RabbitMqTickPublisher.cs` | 修改 | 5 |
| 9 | `Tests/Domain/KBarGeneratorTests.cs` | 修改 | 6 |
| 10 | `Tests/Domain/MultiStockKBarTests.cs` | **新增** | 6 |
| 11 | `Tests/Application/MultiStockOrchestratorTests.cs` | **新增** | 6 |
| 12 | `Tests/Application/MaCrossStrategyMultiStockTests.cs` | **新增** | 6 |

---

## 五、實施順序與依賴關係

```
Phase 2  (KBar + KBarGenerator)
   ↓
Phase 3  (TradingOrchestrator)  ← 依賴 Phase 2
   ↓
Phase 4  (MaCrossStrategy)      ← 依賴 Phase 2 (KBar.Symbol)
   ↓
Phase 1  (Config + Program.cs)  ← 依賴 Phase 3 (Start 簽章)
   ↓
Phase 5  (RabbitMQ RoutingKey)  ← 獨立，可選
   ↓
Phase 6  (測試)                 ← 依賴所有前置 Phase
```

**建議實施順序**：`Phase 2 → 3 → 4 → 1 → 6 → 5`

每完成一個 Phase 應：
1. 確認編譯通過 (`dotnet build`)
2. 執行既有測試 (`dotnet test`)
3. 進行 git commit

---

## 六、不在本次範圍的項目

以下功能有價值但不在此次功能範圍，可作為後續迭代：

| 項目 | 說明 |
|------|------|
| 動態新增/移除訂閱 | 執行中期間透過指令增減股票 |
| 每股獨立策略設定 | 不同股票套用不同策略參數 |
| 上櫃 (OTC) 支援 | 目前 MarketNo 硬編碼為 TWSE |
| 訂閱數量上限保護 | 元大 API 可能有訂閱上限，需查證 |
| 股票代碼驗證 | 檢查股票代碼是否為有效代碼 |
| Best5Quote 多股處理 | Best5Quote 目前無後續處理邏輯 |

---

## 七、風險與注意事項

1. **元大 API 訂閱上限**：需確認元大 OneAPI 同時訂閱的股票數是否有上限，  
   若有則需在 `StockConfig` 加入驗證。

2. **效能影響**：多股同時回報 Tick 時，事件處理在同一 thread 上執行，  
   若訂閱量大需評估是否需要引入並行處理。

3. **記憶體**：每檔股票一個 `KBarGenerator` + 策略的 `List<decimal>`，  
   一般股票數量 (< 100) 不會有問題。

4. **向下相容**：`Start(string[], ...)` 改動會破壞原有 `Start(string, ...)` 簽章，  
   可考慮保留舊簽章作為 overload 轉接。
