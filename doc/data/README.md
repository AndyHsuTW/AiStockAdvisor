# Data 文件

```text
+--------+    +------------+    +-----------+
| Tick   | -> | DDL/Migrate| -> | Timescale |
+--------+    +------------+    +-----------+
```

## Read First

1. `/doc/data/stock-tick-ddl.sql`
2. `/doc/data/timescaledb-migration.md`
3. `/doc/data/stock-tick-response-json-model.md`
4. `setup-timescale.sql`

## Documents

- `/doc/data/stock-tick-ddl.sql`: `stock_ticks` table schema
- `/doc/data/timescaledb-migration.md`: migration 與回滾
- `/doc/data/stock-tick-response-json-model.md`: tick JSON schema
- `setup-timescale.sql`: Timescale setup 指令

## Cross Links

- DBWriter 事故修正：`/doc/messaging/incidents/dbwriter-work-queue-remediation-2026-02-10.md`
- 服務規劃：`/doc/plans/tradingcore-development-plan.md`




