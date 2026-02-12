```text
+------------+    +----------+    +-----------+    +----------+
| tradeDate  | +  | marketNo | +  | stockCode | +  | serialNo |
+------------+    +----------+    +-----------+    +----------+
         \____________________ flowId _____________________/
```

# Log Identity Schema (Tick vs. Non-Tick)

Purpose
- 統一跨系統追蹤欄位，確保交易/行情相關 log 具有可關聯的識別鍵。

Core Identifiers
- tradeDate: 交易日 (台北時區)，格式 YYYY-MM-DD
- marketNo: 市場代碼 (int)
- stockCode: 商品/股票代碼 (string)
- serialNo: 成交序號 (int)
- flowId: 組合識別字串 = {tradeDate}-{marketNo}-{stockCode}-{serialNo}

SerialNo Special Case
- serialNo = -1 表示原始 dwSerialNo = 0xFFFFFFFF
- 定義意義: 商品清盤 (非實際成交序號)
- 即使為 -1 仍可進入 flowId，方便一致追蹤與搜尋

Tick Log Schema (必備欄位)
```
{
  "tradeDate": "2025-01-08",
  "marketNo": 1,
  "stockCode": "2330",
  "serialNo": 12345,
  "flowId": "2025-01-08-1-2330-12345"
}
```

Non-Tick Log Schema (必備欄位)
```
{
  "logId": "GUID"
}
```

Optional Correlation (建議)
- traceId: GUID，同一條流程/請求/任務共用，用於串接多筆 log
- missingFields: string[]，記錄缺失的核心欄位

Notes
- tradeDate 由交易日上下文注入，來源為台北時區交易日。
- 如果沒有 stockCode/marketNo 的系統層 log，仍需提供 logId/traceId 以保留可追蹤性。
