-- TimescaleDB Setup Script for stock_data database
-- Run this after installing TimescaleDB

-- 1. Convert stock_ticks to hypertable
SELECT create_hypertable('stock_ticks', 'time', chunk_time_interval => INTERVAL '1 day');

-- 2. Create indexes
CREATE INDEX idx_stock_ticks_stock_time ON stock_ticks (stock_code, time DESC);
CREATE INDEX idx_stock_ticks_market_stock_date ON stock_ticks (market_no, stock_code, trade_date);
CREATE INDEX idx_stock_ticks_stock_date_time ON stock_ticks (stock_code, trade_date, tick_time);

-- 3. Setup compression policy (compress after 30 days)
ALTER TABLE stock_ticks SET (
    timescaledb.compress,
    timescaledb.compress_segmentby = 'stock_code, market_no'
);
SELECT add_compression_policy('stock_ticks', INTERVAL '30 days');

-- 4. Setup retention policy (keep 5 years)
SELECT add_retention_policy('stock_ticks', INTERVAL '5 years');

-- 5. Create quarantined_messages table
CREATE TABLE IF NOT EXISTS quarantined_messages (
    id BIGSERIAL PRIMARY KEY,
    trade_date DATE,
    market_no SMALLINT,
    stock_code VARCHAR(12),
    serial_no BIGINT,
    reason TEXT NOT NULL,
    payload JSONB NOT NULL,
    quarantined_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS quarantined_messages_trade_market_stock_serial_idx
    ON quarantined_messages (trade_date, market_no, stock_code, serial_no);
