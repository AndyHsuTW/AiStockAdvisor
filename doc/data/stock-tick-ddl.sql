CREATE TABLE IF NOT EXISTS stock_ticks (
    trade_date date NOT NULL,
    market_no smallint NOT NULL,
    stock_code varchar(12) NOT NULL,
    serial_no bigint NOT NULL,
    tick_time time(3) NOT NULL,
    in_out_flag smallint NOT NULL,
    tick_type smallint NOT NULL,
    buy_price_raw int NOT NULL,
    sell_price_raw int NOT NULL,
    deal_price_raw int NOT NULL,
    deal_vol_raw bigint NOT NULL,
    key text,
    tick_time_text text,
    buy_price numeric(18,4),
    sell_price numeric(18,4),
    deal_price numeric(18,4),
    deal_vol bigint,
    is_clearing boolean,
    ingested_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT stock_ticks_pk PRIMARY KEY (trade_date, market_no, stock_code, serial_no),
    CONSTRAINT stock_ticks_in_out_flag_chk CHECK (in_out_flag IN (0,1,2,3,10,11,12,13,14,15)),
    CONSTRAINT stock_ticks_tick_type_chk CHECK (tick_type = 0)
);

CREATE INDEX IF NOT EXISTS stock_ticks_stock_date_time_idx
    ON stock_ticks (stock_code, trade_date, tick_time);

CREATE INDEX IF NOT EXISTS stock_ticks_market_stock_date_idx
    ON stock_ticks (market_no, stock_code, trade_date);

CREATE TABLE IF NOT EXISTS quarantined_messages (
    id bigserial PRIMARY KEY,
    trade_date date,
    market_no smallint,
    stock_code varchar(12),
    serial_no bigint,
    reason text NOT NULL,
    payload jsonb NOT NULL,
    quarantined_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS quarantined_messages_trade_market_stock_serial_idx
    ON quarantined_messages (trade_date, market_no, stock_code, serial_no);
