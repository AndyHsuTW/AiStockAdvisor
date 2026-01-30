# RabbitMQ Publisher 設計文件

## 1. 概述

本文件說明 AiStockAdvisor 專案如何透過 RabbitMQ 發布股票交易資料（Tick 資料），供下游服務（如 StockWriter/DbWriter）消費並寫入資料庫。

## 2. 接收服務分析

### 2.1 RabbitMQ 連線資訊（從 run.env 分析）

| 參數 | 值 | 說明 |
|------|-----|------|
| RABBITMQ_URI | `amqp://dbwriter:love0521@192.168.0.43:5672/%2F` | 連線 URI |
| RABBITMQ_PUBLISHER_USER | `stock-publisher` | 發布者帳號 |
| RABBITMQ_PUBLISHER_PASSWORD | `love0521` | 發布者密碼 |
| QUEUE_NAME | `work-queue` | 消費者佇列名稱 |

### 2.2 Exchange 與 Routing 設定（從 run-e2e.sh 分析）

| 參數 | 值 | 說明 |
|------|-----|------|
| Exchange 名稱 | `stock-ex` | Topic Exchange |
| Routing Key | `stock.twse.tick` | 台股逐筆成交資料路由鍵 |
| 訊息編碼 | Base64 (JSON payload) | 透過 Management API 發送時使用 |

### 2.3 訊息格式（JSON Schema）

接收服務期望的 JSON 格式如下：

```json
{
  "tradeDate": "2026-01-30",
  "key": "1-2330",
  "marketNo": 1,
  "stockCode": "2330",
  "serialNo": 1,
  "tickTime": {
    "hour": 9,
    "minute": 0,
    "second": 1,
    "msec": 123
  },
  "buyPriceRaw": 22250,
  "sellPriceRaw": 22260,
  "dealPriceRaw": 22250,
  "dealVolRaw": 1,
  "inOutFlag": 0,
  "tickType": 0
}
```

#### 欄位說明

| 欄位 | 類型 | 必要 | 說明 |
|------|------|------|------|
| tradeDate | string (YYYY-MM-DD) | ✓ | 交易日期 |
| key | string | ✓ | 鍵值，格式: `{marketNo}-{stockCode}` |
| marketNo | integer | ✓ | 市場代碼 (1=上市, 2=上櫃) |
| stockCode | string | ✓ | 股票代碼 |
| serialNo | integer | ✓ | 逐筆序號 (從 1 開始) |
| tickTime | object | ✓ | 成交時間結構 |
| tickTime.hour | integer | ✓ | 時 (0-23) |
| tickTime.minute | integer | ✓ | 分 (0-59) |
| tickTime.second | integer | ✓ | 秒 (0-59) |
| tickTime.msec | integer | ✓ | 毫秒 (0-999) |
| buyPriceRaw | integer | ✓ | 買價 (原始值, 需除以 10000 得實際價格) |
| sellPriceRaw | integer | ✓ | 賣價 (原始值) |
| dealPriceRaw | integer | ✓ | 成交價 (原始值) |
| dealVolRaw | integer | ✓ | 成交量 |
| inOutFlag | integer | ✓ | 內外盤註記 |
| tickType | integer | ✓ | 明細類別 (0=Normal) |

## 3. 架構設計

### 3.1 元件圖

```
┌─────────────────────────────────────────────────────────────────┐
│                     AiStockAdvisor                              │
├─────────────────────────────────────────────────────────────────┤
│  ┌──────────────────┐    ┌───────────────────────────────────┐  │
│  │ YuantaBrokerClient│───▶│ OnTickReceived Event              │  │
│  │ (Yuanta API)      │    └───────────────────────────────────┘  │
│  └──────────────────┘                    │                      │
│                                          ▼                      │
│                           ┌───────────────────────────────────┐  │
│                           │      ITickPublisher               │  │
│                           │  (Application Layer Interface)    │  │
│                           └───────────────────────────────────┘  │
│                                          │                      │
│                                          ▼                      │
│                           ┌───────────────────────────────────┐  │
│                           │   RabbitMqTickPublisher           │  │
│                           │  (Infrastructure Layer)           │  │
│                           └───────────────────────────────────┘  │
│                                          │                      │
└──────────────────────────────────────────┼──────────────────────┘
                                           │
                                           ▼
                              ┌────────────────────────┐
                              │      RabbitMQ          │
                              │  Exchange: stock-ex    │
                              │  Routing: stock.twse.* │
                              └────────────────────────┘
                                           │
                                           ▼
                              ┌────────────────────────┐
                              │   StockWriter/DbWriter │
                              │   (Consumer Service)   │
                              └────────────────────────┘
                                           │
                                           ▼
                              ┌────────────────────────┐
                              │      PostgreSQL        │
                              │   (stock_data DB)      │
                              └────────────────────────┘
```

### 3.2 專案結構

```
AiStockAdvisor.Application/
  └── Interfaces/
      └── ITickPublisher.cs          # 發布介面定義

AiStockAdvisor.Infrastructure/
  └── Messaging/
      ├── TickMessage.cs             # 訊息 DTO (符合接收服務規格)
      ├── RabbitMqConfig.cs          # RabbitMQ 設定
      └── RabbitMqTickPublisher.cs   # RabbitMQ 發布實作
```

## 4. 實作細節

### 4.1 ITickPublisher 介面

```csharp
public interface ITickPublisher
{
    void Publish(Tick tick);
    void Publish(Tick tick, int buyPriceRaw, int sellPriceRaw, int inOutFlag, int tickType);
}
```

### 4.2 RabbitMQ 連線設定

```csharp
public class RabbitMqConfig
{
    public string Host { get; set; }
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; }
    public string Password { get; set; }
    public string ExchangeName { get; set; } = "stock-ex";
    public string RoutingKey { get; set; } = "stock.twse.tick";
}
```

### 4.3 訊息序列化

使用 `System.Text.Json` (.NET 4.8 可透過 NuGet 安裝) 或手動組裝 JSON 字串。

## 5. 設定方式

### 5.1 環境變數

| 環境變數 | 預設值 | 說明 |
|----------|--------|------|
| RABBITMQ_HOST | 192.168.0.43 | RabbitMQ 主機 |
| RABBITMQ_PORT | 5672 | RabbitMQ 埠號 |
| RABBITMQ_VHOST | / | Virtual Host |
| RABBITMQ_USER | stock-publisher | 使用者名稱 |
| RABBITMQ_PASSWORD | (必填) | 密碼 |
| RABBITMQ_EXCHANGE | stock-ex | Exchange 名稱 |
| RABBITMQ_ROUTING_KEY | stock.twse.tick | Routing Key |

### 5.2 程式碼設定

```csharp
var config = new RabbitMqConfig
{
    Host = "192.168.0.43",
    Port = 5672,
    Username = "stock-publisher",
    Password = "love0521",
    ExchangeName = "stock-ex",
    RoutingKey = "stock.twse.tick"
};

var publisher = new RabbitMqTickPublisher(config, logger);
```

## 6. 使用方式

### 6.1 整合到 YuantaBrokerClient

```csharp
// 在接收到 Tick 時自動發布
brokerClient.OnTickReceived += tick => publisher.Publish(tick);
```

### 6.2 完整 Tick 資訊發布

當需要發布完整的原始欄位時（如買賣價、內外盤註記）：

```csharp
// 解析 Yuanta API 回傳的完整資料後發布
publisher.Publish(tick, buyPriceRaw, sellPriceRaw, inOutFlag, tickType);
```

## 7. 錯誤處理

1. **連線失敗**: 記錄錯誤並嘗試重連
2. **發布失敗**: 記錄錯誤，不阻斷主流程
3. **序列化錯誤**: 記錄詳細錯誤訊息

## 8. 相關文件

- [stock-tick-response-json-model.md](stock-tick-response-json-model.md) - Tick 資料模型規格
- [log-mechanism.md](log-mechanism.md) - 日誌機制說明
