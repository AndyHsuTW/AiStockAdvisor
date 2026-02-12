# 架構文件

```text
+--------+    +---------+    +----------+
| System | -> | Logging | -> | Identity |
+--------+    +---------+    +----------+
```

## Read First

1. `system-architecture.md`
2. `logging-mechanism.md`
3. `logging-identity-schema.md`

## Documents

- `system-architecture.md`: 系統邊界、RabbitMQ 連線與 queue 拓撲
- `logging-mechanism.md`: flow/branch log 設計
- `logging-identity-schema.md`: Tick 與非 Tick 的 log identity schema
- `trading-system.png`: 架構示意圖素材

## Cross Links

- RabbitMQ 操作：`/doc/messaging/rabbitmq-permissions-and-topology.md`
- 資料表與遷移：`/doc/data/timescaledb-migration.md`



