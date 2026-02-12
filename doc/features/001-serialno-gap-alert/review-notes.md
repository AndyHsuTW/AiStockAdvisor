# Code Review 結果與改善建議（SerialNo Gap Alert）

本文件整理針對「SerialNo Gap 檢測與 LINE 通知」功能的 code review 意見，並提供可落地的改善方向。

## [P1] LINE 通知失敗仍會進入冷卻期，可能壓掉後續重要警報

### 現況
`LineAlertService.SendAlertAsync(...)` 目前在進行 HTTP 呼叫之前，就先更新「每個 category 的最後通知時間」並進入冷卻期（cooldown）。

### 風險
若 LINE Notify 發生暫時性錯誤（例如網路不穩、timeout、非 2xx 回應、拋例外），即使這次其實沒有成功送出通知，仍會因冷卻期而抑制下一次通知，降低告警可靠性。

### 改善建議
將「進入冷卻期／更新最後發送時間」改為 **只有在成功送出**（HTTP 2xx）後才更新。

可選方案：
1. **以最後成功時間做節流（建議）**
   - 先檢查 `now - lastSuccessUtc < cooldown`，若仍在冷卻期則跳過。
   - 送出成功後再更新 `lastSuccessUtc`。
2. **兩階段節流（reservation/commit）**
   - 先做「保留」避免同一 category 並發重複送出（可選）。
   - 只有成功才 commit 到「最後成功時間」。

> 目標：失敗不應消耗冷卻配額；成功才會延長冷卻窗口。

## [P2] 冷卻 category 建議使用正規化後的 stock code

### 現況
ConsoleUI 目前用 `category: tick.Symbol` 作為冷卻分類鍵，但 `TickGapDetector` 內部對 symbol 有 `Trim()` 正規化。

### 風險
若來源 tick 的 `Symbol` 偶爾包含前後空白或格式差異（例如 `"2330"` vs `"2330 "`），冷卻鍵會被分裂成不同 bucket，造成：
- 冷卻失效 → 可能通知轟炸
- 冷卻誤判 → 可能錯過應該要送的通知

### 改善建議
category 一律使用已正規化的股票代碼，例如：
- `category: gap.StockCode`（最佳：與檢測結果一致）
- 或 `category: tick.Symbol.Trim()`

## [P3] net48 使用 HttpClient 需確保 `System.Net.Http` 參考被提交

### 現況
`LineAlertService` 引入 `HttpClient`（`System.Net.Http`），在 `net48` 專案中不一定會自動帶入對應參考。

### 風險
若提交時漏掉 `System.Net.Http` 的 `<Reference />`，在部分環境會出現編譯錯誤（找不到 `HttpClient` / `HttpResponseMessage`）。

### 改善建議
確認以下專案檔有包含 `System.Net.Http` 參考，並確實納入 commit：
- `AiStockAdvisor.Infrastructure/AiStockAdvisor.Infrastructure.csproj`
- （測試專案若有用到）`AiStockAdvisor.Tests/AiStockAdvisor.Tests.csproj`

