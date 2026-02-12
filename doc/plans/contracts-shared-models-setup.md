# AiStockAdvisor.Contracts 共用資料模型建立指南

> **文件版本**: 1.0  
> **建立日期**: 2026-02-03  
> **目標專案**: `AiStockAdvisor.Contracts`  
> **技術棧**: C# .NET Standard 2.0  
> **優先級**: 🔴 **前置作業**（TradingCore 開發前必須完成）

---

## 📋 目錄

1. [概述](#1-概述)
2. [專案結構](#2-專案結構)
3. [詳細任務清單](#3-詳細任務清單)
4. [共用類別定義](#4-共用類別定義)
5. [NuGet 套件發布與引用](#5-nuget-套件發布與引用)
6. [驗收標準](#6-驗收標準)

---

## 1. 概述

### 1.1 為什麼需要共用資料模型？

```
┌─────────────────────────────────────────────────────────────────┐
│                      微服務架構                                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ConsoleUI (Publisher)             TradingCore                  │
│  ┌─────────────────────┐          ┌─────────────────────┐       │
│  │ .NET Framework 4.8  │   MQ     │ .NET 8              │       │
│  │ Windows             ├─────────▶│ Linux (Pi5)         │       │
│  │ 發送 TickMessage    │          │ 接收 TickMessage    │       │
│  └─────────────────────┘          └─────────────────────┘       │
│            │                                │                    │
│            │                                │                    │
│            ▼                                ▼                    │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │              需要共用的資料契約                              ││
│  │  - TickMessage (RabbitMQ 訊息格式)                          ││
│  │  - Tick (Domain 模型)                                       ││
│  │  - KBar (K 線資料)                                          ││
│  │  - RabbitMqConfig (連線設定)                                ││
│  └─────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────┘
```

### 1.2 跨框架挑戰

| 服務 | 框架 | 平台 | 可用目標 |
|------|------|------|----------|
| **ConsoleUI (Publisher)** | .NET Framework 4.8 | Windows | ⚠️ netstandard2.0 以下 |
| **DbWriter** | .NET 8 | Linux (Pi5) | ✅ 任意 |
| **TradingCore** | .NET 8 | Linux (Pi5) | ✅ 任意 |
| **Notifier** | .NET 8 | Linux (Pi5) | ✅ 任意 |

> ⚠️ **結論**：必須使用 **.NET Standard 2.0** 作為共用套件目標，才能讓 .NET Framework 4.8 和 .NET 8 都能引用。

### 1.3 方案比較

| 方案 | 優點 | 缺點 | 推薦度 |
|------|------|------|--------|
| **.NET Standard 2.0 套件** | 兩邊都能直接引用，強型別 | 需維護額外專案 | ⭐⭐⭐⭐⭐ |
| **JSON Schema 契約** | 無需共用程式碼 | 弱型別，需各自維護 DTO | ⭐⭐⭐ |
| **Protocol Buffers** | 高效序列化，跨語言 | 學習成本，改動複雜 | ⭐⭐ |

---

## 2. 專案結構

```
AiStockAdvisor.Contracts/
├── AiStockAdvisor.Contracts.csproj    # netstandard2.0
│
├── Models/
│   ├── Tick.cs                        # 逐筆成交資料
│   ├── KBar.cs                        # K 線資料
│   └── Best5Quote.cs                  # 五檔報價
│
├── Messages/
│   ├── TickMessage.cs                 # RabbitMQ Tick 訊息
│   ├── TickTimeInfo.cs                # 時間結構
│   └── TradeEvent.cs                  # 交易事件訊息
│
├── Configuration/
│   ├── RabbitMqConfig.cs              # RabbitMQ 設定
│   └── TimescaleDbConfig.cs           # TimescaleDB 設定
│
└── Constants/
    ├── ExchangeNames.cs               # RabbitMQ Exchange 名稱
    └── QueueNames.cs                  # RabbitMQ Queue 名稱
```

### 2.1 架構圖

```
┌─────────────────────────────────────────────────────────────────┐
│              AiStockAdvisor.Contracts                           │
│              (Target: netstandard2.0)                           │
│                                                                 │
│  ✅ .NET Framework 4.6.1+ 可用                                   │
│  ✅ .NET 8 可用                                                  │
│  ✅ 可發布為 NuGet 套件                                          │
├─────────────────────────────────────────────────────────────────┤
│  Models/          Messages/           Configuration/            │
│  - Tick.cs        - TickMessage.cs    - RabbitMqConfig.cs      │
│  - KBar.cs        - TradeEvent.cs     - TimescaleDbConfig.cs   │
└─────────────────────────────────────────────────────────────────┘
            ▲                           ▲
            │ NuGet 引用                │ NuGet 引用
    ┌───────┴───────┐           ┌───────┴───────┐
    │ ConsoleUI     │           │ TradingCore   │
    │ .NET Fx 4.8   │           │ .NET 8        │
    │ Windows       │           │ Linux (Pi5)   │
    └───────────────┘           └───────────────┘
```

---

## 3. 詳細任務清單

> 💡 **執行順序**：請按照任務編號順序執行，確保前置作業完成後再開發 TradingCore。

---

### Task C.1: 建立 Contracts 專案

**User Story**
> 身為微服務開發者，我需要一個 .NET Standard 2.0 共用套件專案，
> 以便所有服務（無論是 .NET Framework 或 .NET 8）都能引用相同的資料模型，
> 確保 RabbitMQ 訊息格式在生產者與消費者之間保持一致。

**待辦事項**
- [x] 建立 `AiStockAdvisor.Contracts` 專案 (netstandard2.0)
- [x] 設定 NuGet 套件資訊
- [x] 加入必要的 NuGet 套件依賴
- [x] 建立資料夾結構 (Models, Messages, Configuration, Constants)

**專案檔設定**
```xml
<!-- AiStockAdvisor.Contracts.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- 重要：使用 netstandard2.0 確保跨框架相容 -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    
    <!-- NuGet 套件資訊 -->
    <PackageId>AiStockAdvisor.Contracts</PackageId>
    <Version>1.0.0</Version>
    <Authors>AndyHsuTW</Authors>
    <Description>AI Stock Advisor 微服務共用資料模型 (.NET Standard 2.0)</Description>
    
    <!-- 啟用 nullable -->
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- .NET Standard 2.0 需要額外套件支援新語法 -->
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
    <PackageReference Include="Microsoft.Bcl.HashCode" Version="1.1.1" />
    
    <!-- 如需 nullable reference types -->
    <PackageReference Include="Nullable" Version="1.3.1">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

**驗收條件**
| 項目 | 標準 |
|------|------|
| 編譯 | `dotnet build` 成功，無 warning |
| 目標框架 | `netstandard2.0` |
| 資料夾 | 包含 Models, Messages, Configuration, Constants 四個資料夾 |

---

### Task C.2: 實作 Models 類別

**User Story**
> 身為交易系統，我需要 `Tick` 和 `KBar` 等 Domain 模型，
> 以便在記憶體中表示市場資料，
> 並支援技術指標計算和交易決策。

**待辦事項**
- [x] 實作 `Tick.cs` - 逐筆成交資料
- [x] 實作 `KBar.cs` - K 線資料
- [x] 實作 `Best5Quote.cs` - 五檔報價（可選）
- [x] 單元測試

**程式碼詳見**：[4.1 Tick 類別](#41-tick-類別)、[4.2 KBar 類別](#42-kbar-類別)

**驗收條件**
| 項目 | 標準 |
|------|------|
| Tick | 包含 MarketNo, Symbol, Time, Price, Volume 屬性 |
| KBar | 包含 Time, OHLCV 屬性，以及 Body, UpperShadow, LowerShadow 計算屬性 |
| 語法相容 | 不使用 `init`、`record`、`required` 等 .NET Standard 2.0 不支援的語法 |

---

### Task C.3: 實作 Messages 類別

**User Story**
> 身為 RabbitMQ 生產者與消費者，我需要共用的訊息格式定義，
> 以便 Publisher 發送的 JSON 可以被 TradingCore 正確反序列化，
> 確保資料傳輸的一致性。

**待辦事項**
- [x] 實作 `TickMessage.cs` - RabbitMQ Tick 訊息
- [x] 實作 `TickTimeInfo.cs` - 時間結構
- [x] 實作 `TradeEvent.cs` - 交易事件訊息
- [x] 單元測試 (JSON 序列化/反序列化)

**程式碼詳見**：[4.3 TickMessage 類別](#43-tickmessage-類別)

**驗收條件**
| 項目 | 標準 |
|------|------|
| TickMessage | 符合現有 Publisher 的 JSON 格式 |
| ToDomainTick() | 可正確轉換為 Tick Domain 物件 |
| JSON 相容 | 使用 System.Text.Json 序列化/反序列化成功 |

---

### Task C.4: 實作 Configuration 類別

**User Story**
> 身為微服務，我需要共用的設定模型，
> 以便從 appsettings.json 載入 RabbitMQ 和 TimescaleDB 連線資訊，
> 確保所有服務使用相同的設定結構。

**待辦事項**
- [x] 實作 `RabbitMqConfig.cs` - RabbitMQ 連線設定
- [x] 實作 `TimescaleDbConfig.cs` - TimescaleDB 連線設定
- [x] 單元測試

**程式碼詳見**：[4.4 RabbitMqConfig 類別](#44-rabbitmqconfig-類別)

**驗收條件**
| 項目 | 標準 |
|------|------|
| RabbitMqConfig | 包含 Host, Port, VirtualHost, Username, Password |
| TimescaleDbConfig | 包含 ConnectionString 或個別連線參數 |
| 預設值 | Host 預設為 "192.168.0.43" (Pi5) |

---

### Task C.5: 實作 Constants 類別

**User Story**
> 身為開發者，我需要集中定義 RabbitMQ Exchange 和 Queue 名稱，
> 以便避免在各服務中硬編碼字串，
> 減少打字錯誤導致的訊息路由失敗。

**待辦事項**
- [x] 實作 `ExchangeNames.cs` - RabbitMQ Exchange 名稱常數
- [x] 實作 `QueueNames.cs` - RabbitMQ Queue 名稱常數

**實作範例**
```csharp
namespace AiStockAdvisor.Contracts.Constants
{
    public static class ExchangeNames
    {
        /// <summary>即時 Tick 資料 Exchange</summary>
        public const string StockTicks = "stock.ticks";
        
        /// <summary>交易事件 Exchange</summary>
        public const string TradingEvents = "trading.events";
    }
    
    public static class QueueNames
    {
        /// <summary>TradingCore 消費 Tick 的 Queue</summary>
        public const string TradingCoreTicks = "trading-core.ticks";
        
        /// <summary>DbWriter 消費 Tick 的 Queue</summary>
        public const string DbWriterTicks = "db-writer.ticks";
    }
}
```

**驗收條件**
| 項目 | 標準 |
|------|------|
| ExchangeNames | 包含 StockTicks, TradingEvents |
| QueueNames | 包含 TradingCoreTicks, DbWriterTicks |

---

### Task C.6: 發布 NuGet 套件

**User Story**
> 身為發布工程師，我需要將 Contracts 打包為 NuGet 套件，
> 以便其他專案可以透過 NuGet 引用，
> 而不需要直接參考專案原始碼。

**待辦事項**
- [x] 設定 BaGet NuGet 伺服器連線
- [x] 建立打包與推送腳本
- [x] 測試在 .NET Framework 4.8 專案引用
- [x] 測試在 .NET 8 專案引用

**BaGet 伺服器資訊**
| 項目 | 值 |
|------|----|
| URL | http://192.168.0.43:5555/ |
| API Key | 環境變數 `BAGET_API_KEY` |

**程式碼詳見**：[5. NuGet 套件發布與引用](#5-nuget-套件發布與引用)

**驗收條件**
| 項目 | 標準 |
|------|------|
| 打包成功 | 產出 `AiStockAdvisor.Contracts.1.0.0.nupkg` |
| 推送成功 | 套件已上傳至 BaGet (http://192.168.0.43:5555/) |
| .NET Fx 引用 | ConsoleUI 專案可正常引用並編譯 |
| .NET 8 引用 | 新建 .NET 8 專案可正常引用並編譯 |

---

### Task C.7: 整合現有 Publisher

**User Story**
> 身為 ConsoleUI (Publisher) 專案，我需要改用 Contracts 套件的 TickMessage，
> 以便移除重複的類別定義，
> 並確保與其他服務使用完全相同的訊息格式。

**待辦事項**
- [x] 在 ConsoleUI 專案安裝 `AiStockAdvisor.Contracts` NuGet 套件
- [x] 移除 `Infrastructure/Messaging/TickMessage.cs` 原有定義
- [x] 更新 using 語句指向 `AiStockAdvisor.Contracts.Messages`
- [x] 編譯驗證
- [ ] 整合測試 (發送訊息到 RabbitMQ)

**驗收條件**
| 項目 | 標準 |
|------|------|
| 編譯 | ConsoleUI 專案使用新套件編譯成功 |
| 訊息格式 | 發送的 JSON 格式與原本相同 |
| 無重複 | 移除原本的 TickMessage.cs |

---

## 4. 共用類別定義

> ⚠️ **語法限制**：
> - 不使用 `init` 屬性 → 改用建構函式 + 唯讀屬性
> - 不使用 `record` 類型 → 改用傳統 `class`
> - 不使用 `required` 修飾詞 → 改用建構函式參數驗證
> - 不使用 file-scoped namespace → 改用傳統大括號 namespace

---

### 4.1 Tick 類別

```csharp
using System;

namespace AiStockAdvisor.Contracts.Models
{
    /// <summary>
    /// 代表市場的逐筆成交資訊 (Tick)
    /// .NET Standard 2.0 相容版本
    /// </summary>
    public class Tick
    {
        /// <summary>市場代碼 (1=上市, 2=上櫃)</summary>
        public int MarketNo { get; }
        
        /// <summary>股票代碼</summary>
        public string Symbol { get; }
        
        /// <summary>成交時間</summary>
        public DateTime Time { get; }
        
        /// <summary>交易日期</summary>
        public DateTime TradeDate { get; }
        
        /// <summary>逐筆序號</summary>
        public int SerialNo { get; }
        
        /// <summary>成交價格</summary>
        public decimal Price { get; }
        
        /// <summary>成交單量（張）</summary>
        public decimal Volume { get; }
        
        public Tick(int marketNo, string symbol, DateTime time, DateTime tradeDate, 
                    int serialNo, decimal price, decimal volume)
        {
            MarketNo = marketNo;
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Time = time;
            TradeDate = tradeDate;
            SerialNo = serialNo;
            Price = price;
            Volume = volume;
        }
        
        // 簡化建構函式
        public Tick(string symbol, DateTime time, decimal price, decimal volume)
            : this(0, symbol, time, time.Date, 0, price, volume) { }
        
        public override string ToString() 
            => $"[{Time:HH:mm:ss}] {Symbol} @ {Price} (Vol: {Volume})";
    }
}
```

---

### 4.2 KBar 類別

```csharp
using System;

namespace AiStockAdvisor.Contracts.Models
{
    /// <summary>
    /// 代表一根 K 線 (K-Bar)，包含特定時間區間內的 OHLCV 資訊
    /// .NET Standard 2.0 相容版本
    /// </summary>
    public class KBar
    {
        /// <summary>K 線區間的結束時間</summary>
        public DateTime Time { get; }
        
        /// <summary>開盤價</summary>
        public decimal Open { get; }
        
        /// <summary>最高價</summary>
        public decimal High { get; }
        
        /// <summary>最低價</summary>
        public decimal Low { get; }
        
        /// <summary>收盤價</summary>
        public decimal Close { get; }
        
        /// <summary>成交量</summary>
        public decimal Volume { get; }
        
        public KBar(DateTime time, decimal open, decimal high, decimal low, decimal close, decimal volume)
        {
            if (high < low)
                throw new ArgumentException("High price cannot be less than Low price");
            
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }
        
        /// <summary>實體長度（絕對值）</summary>
        public decimal Body => Math.Abs(Close - Open);
        
        /// <summary>上影線長度</summary>
        public decimal UpperShadow => High - Math.Max(Open, Close);
        
        /// <summary>下影線長度</summary>
        public decimal LowerShadow => Math.Min(Open, Close) - Low;
        
        /// <summary>是否為陽線</summary>
        public bool IsBullish => Close > Open;
        
        /// <summary>是否為陰線</summary>
        public bool IsBearish => Close < Open;
    }
}
```

---

### 4.3 TickMessage 類別

```csharp
using System;

namespace AiStockAdvisor.Contracts.Messages
{
    /// <summary>
    /// RabbitMQ Tick 訊息格式
    /// 符合 Publisher/DbWriter 服務的 JSON 結構
    /// </summary>
    public class TickMessage
    {
        /// <summary>交易日期 (YYYY-MM-DD)</summary>
        public string TradeDate { get; set; } = string.Empty;
        
        /// <summary>鍵值，格式: {marketNo}-{stockCode}</summary>
        public string Key { get; set; } = string.Empty;
        
        /// <summary>市場代碼 (1=上市, 2=上櫃)</summary>
        public int MarketNo { get; set; }
        
        /// <summary>股票代碼</summary>
        public string StockCode { get; set; } = string.Empty;
        
        /// <summary>逐筆序號</summary>
        public int SerialNo { get; set; }
        
        /// <summary>成交時間結構</summary>
        public TickTimeInfo TickTime { get; set; } = new TickTimeInfo();
        
        /// <summary>買價原始值 (需除以 10000)</summary>
        public int BuyPriceRaw { get; set; }
        
        /// <summary>賣價原始值 (需除以 10000)</summary>
        public int SellPriceRaw { get; set; }
        
        /// <summary>成交價原始值 (需除以 10000)</summary>
        public int DealPriceRaw { get; set; }
        
        /// <summary>成交量</summary>
        public int DealVolRaw { get; set; }
        
        /// <summary>內外盤註記 (0=內盤, 1=外盤)</summary>
        public int InOutFlag { get; set; }
        
        /// <summary>明細類別</summary>
        public int TickType { get; set; }
        
        // 計算屬性
        public decimal BuyPrice => BuyPriceRaw / 10000m;
        public decimal SellPrice => SellPriceRaw / 10000m;
        public decimal DealPrice => DealPriceRaw / 10000m;
        
        /// <summary>轉換為 Domain Tick</summary>
        public Tick ToDomainTick()
        {
            var time = new DateTime(
                TickTime.Year, TickTime.Month, TickTime.Day,
                TickTime.Hour, TickTime.Minute, TickTime.Second,
                TickTime.Millisecond);
            
            return new Tick(
                marketNo: MarketNo,
                symbol: StockCode,
                time: time,
                tradeDate: DateTime.Parse(TradeDate),
                serialNo: SerialNo,
                price: DealPrice,
                volume: DealVolRaw);
        }
    }
    
    /// <summary>
    /// Tick 時間結構
    /// </summary>
    public class TickTimeInfo
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int Second { get; set; }
        public int Millisecond { get; set; }
    }
}
```

---

### 4.4 RabbitMqConfig 類別

```csharp
namespace AiStockAdvisor.Contracts.Configuration
{
    /// <summary>
    /// RabbitMQ 連線設定
    /// </summary>
    public class RabbitMqConfig
    {
        public string Host { get; set; } = "192.168.0.43";
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; } = "/";
        public string Username { get; set; } = "admin";
        public string Password { get; set; } = string.Empty;
        
        // Exchange 名稱
        public string TickExchange { get; set; } = "stock.ticks";
        public string EventExchange { get; set; } = "trading.events";
        
        // Queue 名稱
        public string TickQueue { get; set; } = "trading-core.ticks";
    }
}
```

---

### 4.5 TimescaleDbConfig 類別

```csharp
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
```

---

### 4.6 TradeEvent 類別

```csharp
using System;

namespace AiStockAdvisor.Contracts.Messages
{
    /// <summary>
    /// 交易事件訊息（發布至 RabbitMQ）
    /// </summary>
    public class TradeEvent
    {
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;  // OrderCreated, OrderFilled, PositionOpened, PositionClosed
        public DateTime Timestamp { get; set; }
        
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;       // Buy, Sell
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        
        public string TriggerRule { get; set; } = string.Empty;
        public string ExecutionMode { get; set; } = string.Empty;  // Simulated, LineNotify, Real
        
        /// <summary>附加資訊 (JSON 格式)</summary>
        public string Metadata { get; set; } = string.Empty;
    }
}
```

---

## 5. NuGet 套件發布與引用

> 📦 **BaGet 伺服器**：http://192.168.0.43:5555/ (架設於 Raspberry Pi 5)

### 5.1 環境設定

#### Windows 環境變數設定

```powershell
# 設定 API Key 環境變數 (PowerShell)
$env:BAGET_API_KEY = "4757fc23e404d20accd36524e0dc1b445a38c8a69dc0ad30fb2c0808ed95202a"

# 或永久設定 (系統環境變數)
[System.Environment]::SetEnvironmentVariable("BAGET_API_KEY", "4757fc23e404d20accd36524e0dc1b445a38c8a69dc0ad30fb2c0808ed95202a", "User")
```

#### 註冊 BaGet NuGet 源 (一次性設定)

```powershell
# Windows / Pi5 皆適用
dotnet nuget add source http://192.168.0.43:5555/v3/index.json -n BaGet

# 驗證來源
dotnet nuget list source
```

### 5.2 打包與推送套件

```powershell
# 1. 進入專案目錄
cd AiStockAdvisor.Contracts

# 2. 打包
dotnet pack -c Release

# 3. 推送至 BaGet
dotnet nuget push ./bin/Release/AiStockAdvisor.Contracts.1.0.0.nupkg `
    --source http://192.168.0.43:5555/v3/index.json `
    --api-key $env:BAGET_API_KEY

# 或使用簡短版本（已註冊 source 名稱）
dotnet nuget push ./bin/Release/*.nupkg -s BaGet -k $env:BAGET_API_KEY
```

### 5.3 .NET Framework 4.8 專案引用

```powershell
# 使用 Package Manager Console
Install-Package AiStockAdvisor.Contracts -Source BaGet
```

或在 `NuGet.config` 中加入：
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="BaGet" value="http://192.168.0.43:5555/v3/index.json" allowInsecureConnections="true" />
  </packageSources>
</configuration>
```

然後編輯 `.csproj`：
```xml
<PackageReference Include="AiStockAdvisor.Contracts" Version="1.0.0" />
```

### 5.4 .NET 8 專案引用

```bash
# 從 BaGet 安裝
dotnet add package AiStockAdvisor.Contracts --source http://192.168.0.43:5555/v3/index.json

# 或已註冊 source 後
dotnet add package AiStockAdvisor.Contracts
```

或手動編輯 `.csproj`：
```xml
<PackageReference Include="AiStockAdvisor.Contracts" Version="1.0.*" />
```

### 5.5 版本更新流程

```powershell
# 1. 更新版本號 (AiStockAdvisor.Contracts.csproj)
#    <Version>1.0.1</Version>

# 2. 重新打包並推送
dotnet pack -c Release
dotnet nuget push ./bin/Release/AiStockAdvisor.Contracts.1.0.1.nupkg -s BaGet -k $env:BAGET_API_KEY

# 3. 各專案更新套件
dotnet add package AiStockAdvisor.Contracts --version 1.0.1
```

### 5.6 BaGet 管理介面

- **套件瀏覽**：http://192.168.0.43:5555/
- **API 端點**：http://192.168.0.43:5555/v3/index.json
- **搜尋套件**：http://192.168.0.43:5555/v3/search

---

## 6. 驗收標準

### 6.1 整體驗收

| 項目 | 標準 |
|------|------|
| 編譯 | `dotnet build` 成功，無 warning |
| 目標框架 | `netstandard2.0` |
| .NET Fx 相容 | 在 .NET Framework 4.8 專案可引用並編譯 |
| .NET 8 相容 | 在 .NET 8 專案可引用並編譯 |
| JSON 序列化 | TickMessage 可正確序列化/反序列化 |

### 6.2 測試案例

```csharp
// 測試 1: Tick 建立
var tick = new Tick("2327", DateTime.Now, 500.5m, 100);
Assert.Equal("2327", tick.Symbol);
Assert.Equal(500.5m, tick.Price);

// 測試 2: KBar 計算屬性
var kbar = new KBar(DateTime.Now, 500, 510, 495, 505, 1000);
Assert.Equal(5m, kbar.Body);
Assert.Equal(5m, kbar.UpperShadow);
Assert.Equal(5m, kbar.LowerShadow);
Assert.True(kbar.IsBullish);

// 測試 3: TickMessage 轉換
var message = new TickMessage 
{ 
    StockCode = "2327", 
    DealPriceRaw = 5005000,  // 500.5 * 10000
    TickTime = new TickTimeInfo { Year = 2026, Month = 2, Day = 3, Hour = 9, Minute = 30, Second = 0 }
};
var domain = message.ToDomainTick();
Assert.Equal(500.5m, domain.Price);

// 測試 4: JSON 往返
var json = JsonSerializer.Serialize(message);
var parsed = JsonSerializer.Deserialize<TickMessage>(json);
Assert.Equal(message.StockCode, parsed.StockCode);
```

---

## 附錄 A: .NET Standard 2.0 語法限制速查

| C# 語法 | 支援情況 | 替代方案 |
|---------|----------|----------|
| `init` 屬性 | ❌ 不支援 | 使用建構函式 + 唯讀屬性 |
| `record` 類型 | ❌ 不支援 | 使用傳統 `class` |
| `required` 修飾詞 | ❌ 不支援 | 建構函式參數驗證 |
| file-scoped namespace | ❌ 不支援 | 傳統大括號 `namespace { }` |
| nullable reference types | ⚠️ 需套件 | 安裝 `Nullable` NuGet 套件 |
| `Index` / `Range` (^, ..) | ⚠️ 需套件 | 安裝 `IndexRange` NuGet 套件 |
| `default` 介面實作 | ❌ 不支援 | 使用抽象類別 |
| pattern matching 增強 | ⚠️ 部分 | 使用基本 switch |

---

## 附錄 B: 相關文件

| 文件 | 說明 |
|------|------|
| [/doc/plans/tradingcore-development-plan.md](/doc/plans/tradingcore-development-plan.md) | TradingCore 服務完整開發計劃 |
| [/doc/data/stock-tick-response-json-model.md](/doc/data/stock-tick-response-json-model.md) | Tick 訊息 JSON 格式規格 |
| [/doc/messaging/rabbitmq-publisher-design.md](/doc/messaging/rabbitmq-publisher-design.md) | RabbitMQ Publisher 設計文件 |
| [/doc/architecture/system-architecture.md](/doc/architecture/system-architecture.md) | 系統架構總覽 |

---

*文件結束*


