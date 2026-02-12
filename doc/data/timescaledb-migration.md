# TimescaleDB Migration 指南

> **文件版本**: 1.0  
> **建立日期**: 2026-01-31  
> **狀態**: 待執行

---

## 📋 概述

本文件記錄從 PostgreSQL 17 升級至 TimescaleDB 的變更，以及 DbWriter Service 需要配合調整的內容。

---

## 1. 資料庫變更

### 1.1 Docker Image 變更

| 項目 | 變更前 | 變更後 |
|------|--------|--------|
| Image | `postgres:17` | `timescale/timescaledb:latest-pg17` |
| 擴展 | 無 | TimescaleDB |

### 1.2 Schema 變更

#### stock_ticks 表

| 變更項目 | 說明 |
|----------|------|
| 新增欄位 | `time TIMESTAMPTZ NOT NULL` |
| Primary Key | 從 `(trade_date, market_no, stock_code, serial_no)` 改為 `(time, market_no, stock_code, serial_no)` |
| 表類型 | 從普通表轉換為 TimescaleDB 超表 (Hypertable) |
| 分區策略 | 以 `time` 欄位分區，每天一個 chunk |
| 壓縮策略 | 30 天後自動壓縮 |
| 保留策略 | 5 年後自動刪除 |

#### 新 Schema

```sql
CREATE TABLE stock_ticks (
    time           TIMESTAMPTZ NOT NULL,          -- 新增：組合 trade_date + tick_time
    trade_date     DATE NOT NULL,
    market_no      SMALLINT NOT NULL,
    stock_code     VARCHAR(12) NOT NULL,
    serial_no      BIGINT NOT NULL,
    tick_time      TIME(3) NOT NULL,
    in_out_flag    SMALLINT NOT NULL,
    tick_type      SMALLINT NOT NULL,
    buy_price_raw  INT NOT NULL,
    sell_price_raw INT NOT NULL,
    deal_price_raw INT NOT NULL,
    deal_vol_raw   BIGINT NOT NULL,
    key            TEXT,
    tick_time_text TEXT,
    buy_price      NUMERIC(18,4),
    sell_price     NUMERIC(18,4),
    deal_price     NUMERIC(18,4),
    deal_vol       BIGINT,
    is_clearing    BOOLEAN,
    ingested_at    TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT stock_ticks_pk PRIMARY KEY (time, market_no, stock_code, serial_no),
    CONSTRAINT stock_ticks_in_out_flag_chk CHECK (in_out_flag IN (0,1,2,3,10,11,12,13,14,15)),
    CONSTRAINT stock_ticks_tick_type_chk CHECK (tick_type = 0)
);

-- 轉換為超表
SELECT create_hypertable('stock_ticks', 'time', chunk_time_interval => INTERVAL '1 day');

-- 索引
CREATE INDEX idx_stock_ticks_stock_time ON stock_ticks (stock_code, time DESC);
CREATE INDEX idx_stock_ticks_market_stock_date ON stock_ticks (market_no, stock_code, trade_date);
CREATE INDEX idx_stock_ticks_stock_date_time ON stock_ticks (stock_code, trade_date, tick_time);

-- 壓縮策略
ALTER TABLE stock_ticks SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'stock_code, market_no'
);
SELECT add_compression_policy('stock_ticks', INTERVAL '30 days');

-- 保留策略
SELECT add_retention_policy('stock_ticks', INTERVAL '5 years');
```

---

## 2. DbWriter Service 需要的變更

### 2.1 新增 `time` 欄位計算

DbWriter 在寫入資料時，需要計算新的 `time` 欄位：

```csharp
// TickMessage 或 Entity 中新增 time 欄位
public DateTimeOffset Time { get; set; }

// 計算 time 值（組合 trade_date + tick_time）
// tick_time 是 TimeOnly 或 TimeSpan 類型
public DateTimeOffset CalculateTime(DateOnly tradeDate, TimeOnly tickTime)
{
    // 台灣時區
    var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
    
    // 組合日期和時間
    var dateTime = tradeDate.ToDateTime(tickTime);
    
    // 轉換為 DateTimeOffset（帶時區）
    return new DateTimeOffset(dateTime, taipeiTimeZone.BaseUtcOffset);
}

// 或者如果 tick_time 是 TimeSpan：
public DateTimeOffset CalculateTime(DateTime tradeDate, TimeSpan tickTime)
{
    var taipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
    var dateTime = tradeDate.Date.Add(tickTime);
    return new DateTimeOffset(dateTime, taipeiTimeZone.BaseUtcOffset);
}
```

### 2.2 更新 INSERT 語句

#### 變更前

```sql
INSERT INTO stock_ticks (
    trade_date, market_no, stock_code, serial_no, tick_time,
    in_out_flag, tick_type, buy_price_raw, sell_price_raw, 
    deal_price_raw, deal_vol_raw, key, tick_time_text,
    buy_price, sell_price, deal_price, deal_vol, is_clearing
) VALUES (...)
```

#### 變更後

```sql
INSERT INTO stock_ticks (
    time,  -- 新增
    trade_date, market_no, stock_code, serial_no, tick_time,
    in_out_flag, tick_type, buy_price_raw, sell_price_raw, 
    deal_price_raw, deal_vol_raw, key, tick_time_text,
    buy_price, sell_price, deal_price, deal_vol, is_clearing
) VALUES (...)
```

### 2.3 Dapper/EF Core 範例

#### 使用 Dapper

```csharp
public async Task InsertTickAsync(StockTick tick)
{
    // 計算 time 欄位
    tick.Time = CalculateTime(tick.TradeDate, tick.TickTime);
    
    const string sql = @"
        INSERT INTO stock_ticks (
            time, trade_date, market_no, stock_code, serial_no, tick_time,
            in_out_flag, tick_type, buy_price_raw, sell_price_raw,
            deal_price_raw, deal_vol_raw, key, tick_time_text,
            buy_price, sell_price, deal_price, deal_vol, is_clearing
        ) VALUES (
            @Time, @TradeDate, @MarketNo, @StockCode, @SerialNo, @TickTime,
            @InOutFlag, @TickType, @BuyPriceRaw, @SellPriceRaw,
            @DealPriceRaw, @DealVolRaw, @Key, @TickTimeText,
            @BuyPrice, @SellPrice, @DealPrice, @DealVol, @IsClearing
        )
        ON CONFLICT (time, market_no, stock_code, serial_no) DO NOTHING";
    
    await connection.ExecuteAsync(sql, tick);
}
```

#### 批次寫入

```csharp
public async Task InsertTicksBatchAsync(IEnumerable<StockTick> ticks)
{
    // 批次計算 time 欄位
    foreach (var tick in ticks)
    {
        tick.Time = CalculateTime(tick.TradeDate, tick.TickTime);
    }
    
    // 使用 COPY 或批次 INSERT 提升效能
    // ...
}
```

### 2.4 Entity/Model 變更

```csharp
public class StockTick
{
    // 新增欄位
    public DateTimeOffset Time { get; set; }
    
    // 既有欄位
    public DateOnly TradeDate { get; set; }
    public short MarketNo { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public long SerialNo { get; set; }
    public TimeOnly TickTime { get; set; }
    public short InOutFlag { get; set; }
    public short TickType { get; set; }
    public int BuyPriceRaw { get; set; }
    public int SellPriceRaw { get; set; }
    public int DealPriceRaw { get; set; }
    public long DealVolRaw { get; set; }
    public string? Key { get; set; }
    public string? TickTimeText { get; set; }
    public decimal? BuyPrice { get; set; }
    public decimal? SellPrice { get; set; }
    public decimal? DealPrice { get; set; }
    public long? DealVol { get; set; }
    public bool? IsClearing { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}
```

---

## 3. RabbitMQ TickMessage 變更

如果 TickMessage 需要包含 `time` 欄位：

### 選項 A：在 Publisher 端計算（推薦）

```csharp
// AiStockAdvisor.Infrastructure/Messaging/TickMessage.cs
public class TickMessage
{
    // 新增
    public DateTimeOffset Time { get; set; }
    
    // 既有欄位...
}

// 在發布時計算
public void Publish(StockTick tick)
{
    var message = new TickMessage
    {
        Time = CalculateTime(tick.TradeDate, tick.TickTime),
        TradeDate = tick.TradeDate,
        // ...
    };
    
    _channel.BasicPublish(...);
}
```

### 選項 B：在 Consumer/DbWriter 端計算

```csharp
// DbWriter 收到訊息後計算
public async Task HandleMessageAsync(TickMessage message)
{
    var tick = new StockTick
    {
        Time = CalculateTime(message.TradeDate, message.TickTime),
        TradeDate = message.TradeDate,
        // ...
    };
    
    await _repository.InsertAsync(tick);
}
```

**建議**：選項 B 較簡單，不需要修改現有的 Publisher。

---

## 4. 查詢最佳化建議

### 4.1 利用時間範圍查詢

```sql
-- ✅ 高效：使用 time 欄位（會利用超表分區）
SELECT * FROM stock_ticks 
WHERE stock_code = '2327' 
  AND time >= NOW() - INTERVAL '1 hour';

-- ⚠️ 較慢：不使用 time 欄位
SELECT * FROM stock_ticks 
WHERE stock_code = '2327' 
  AND trade_date = CURRENT_DATE;
```

### 4.2 連續聚合（未來可選）

```sql
-- 建立 1 分鐘 K 線的連續聚合
CREATE MATERIALIZED VIEW kbars_1min
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 minute', time) AS bucket,
    stock_code,
    first(deal_price, time) AS open,
    max(deal_price) AS high,
    min(deal_price) AS low,
    last(deal_price, time) AS close,
    sum(deal_vol) AS volume
FROM stock_ticks
GROUP BY bucket, stock_code;

-- 自動刷新策略
SELECT add_continuous_aggregate_policy('kbars_1min',
    start_offset => INTERVAL '1 hour',
    end_offset => INTERVAL '1 minute',
    schedule_interval => INTERVAL '1 minute'
);
```

---

## 5. 驗證清單

升級完成後，請驗證：

- [ ] TimescaleDB 擴展已安裝：`SELECT extname FROM pg_extension WHERE extname = 'timescaledb';`
- [ ] stock_ticks 是超表：`SELECT * FROM timescaledb_information.hypertables;`
- [ ] 壓縮策略已設定：`SELECT * FROM timescaledb_information.compression_settings;`
- [ ] 保留策略已設定：`SELECT * FROM timescaledb_information.jobs WHERE proc_name = 'policy_retention';`
- [ ] DbWriter 可正常寫入資料
- [ ] 資料可正常查詢

---

## 6. 回滾計畫

如需回滾：

1. 停止 TimescaleDB 容器
2. 修改 docker-compose.yml 改回 `postgres:17`
3. 清空資料目錄
4. 啟動容器
5. 執行原始 DDL（不含 TimescaleDB 特定語法）

---

*文件結束*
