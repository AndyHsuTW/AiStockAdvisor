```text
+----------+    +--------+    +--------+    +----------+
| flowId   | -> | logId  | -> | spanId | -> | parentId |
+----------+    +--------+    +--------+    +----------+
```

# Log Mechanism (Flow + Branch)

Goal
- 在多層函數呼叫與多分支情境下，讓 log 具備可追蹤性與可關聯性。

Core Concepts
- logId: 流程識別 (同一條流程共用)
- spanId: 分支識別 (每個分支唯一)
- parentSpanId: 分支來源 (可追溯呼叫樹)
- flowId: 交易識別 (tradeDate + marketNo + stockCode + serialNo)
- traceId: 跨系統追蹤 (選用，通常由上游系統提供)

Rules
1) 同一流程只建立一個 logId
2) 每次產生分支就建立新的 spanId
3) 分支的 parentSpanId = 產生分支時的當前 spanId
4) flowId 只用於 tick 交易資料，與 logId/spanId 並存

API (Reusable Library)
- Namespace: AiStockAdvisor.Logging
- Files: AiStockAdvisor.Logging/LogScope.cs, AiStockAdvisor.Logging/LogIdentity.cs

Reuse In Other Projects
- 將 AiStockAdvisor.Logging 專案加入方案，或直接引用 AiStockAdvisor.Logging.csproj
- 只依賴 .NET Standard 2.0，不需要其他專案依賴

Usage
```
// 進入流程 (root)
using (LogScope.BeginFlow())
{
    var logId = LogScope.CurrentLogId;
    var spanId = LogScope.CurrentSpanId;
    logger.LogInformation(LogScope.FormatMessage("start"));

    // 分支 A
    using (LogScope.BeginBranch())
    {
        logger.LogInformation(LogScope.FormatMessage("branch A"));
    }

    // 分支 B
    using (LogScope.BeginBranch())
    {
        logger.LogInformation(LogScope.FormatMessage("branch B"));
    }
}
```

Asynchronous / Threading Notes
- async/await 與 Task.Run 會繼承 AsyncLocal 的 logId/spanId。
- 若使用 new Thread，需手動傳遞：
  - 保存 CurrentLogId/CurrentSpanId/CurrentParentSpanId
  - 於新執行緒使用 LogScope.Use(logId, spanId, parentSpanId)

SDK Callback / No Root
- 若進入點沒有 root logId（例如 SDK event callback），使用 LogScope.EnsureFlow() 建立 root logId/spanId

Other Entry Points
- 例如 Console/Service 啟動點，可使用 LogScope.BeginFlow() 作為整體流程 root

Tick Identity
```
var identity = LogIdentity.ForTick(tradeDate, marketNo, stockCode, serialNo);
var identityJson = identity.ToJson();
```

Non-Tick Identity
```
var identity = LogIdentity.ForNonTick();
var identityJson = identity.ToJson();
```

Output Example
```
[logId=... spanId=... parentSpanId=...] [Orchestrator] Starting trading flow...
```
