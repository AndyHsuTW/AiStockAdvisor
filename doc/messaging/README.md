# Messaging 文件

```text
+-----------+    +----------+    +------------+
| Publisher | -> | stock-ex | -> | work-queue |
+-----------+    +----------+    +------------+
                                      |
                                      v
                                 +----------+
                                 | DBWriter |
                                 +----------+
```

## Read First

1. `rabbitmq-permissions-and-topology.md`
2. `/doc/messaging/rabbitmq-publisher-design.md`
3. `rabbitmq-tradingcore-cli.md`
4. `incidents/dbwriter-work-queue-remediation-2026-02-10.md`

## Documents

- `rabbitmq-permissions-and-topology.md`: 權限與拓撲基線
- `/doc/messaging/rabbitmq-publisher-design.md`: publisher 設計與 payload
- `rabbitmq-tradingcore-cli.md`: tradingcore queue/權限 CLI
- `incidents/`: 事故與修正計劃

## Cross Links

- 系統架構：`/doc/architecture/system-architecture.md`
- DB 寫入規格：`/doc/data/stock-tick-ddl.sql`




