# 測試指南 (Testing Guide)

本文件說明如何啟動並測試 **AiStockAdvisor** 系統。

## 1. 環境準備 (Prerequisites)

在開始之前，請確保您的環境滿足以下條件：

1.  **憑證 (Certificates)**:
    *   請確認您的目錄下擁有有效的元大 API 憑證檔案 (`.pfx`)。
    *   系統預設會尋找執行目錄下的憑證，或根據 `Yuanta.config` (如果有的話) 進行載入。
    *   **重要**: 如果您在測試環境 (UAT)，請確保相關憑證有效。

2.  **依賴檔案 (DLLs)**:
    *   系統建置時會自動將必要的 DLL (`YuantaOneAPI.dll`, `YuantaCAPIDLL64.dll`, `APICOMCHECK64.dll`) 複製到輸出目錄。無需手動複製。

## 2. 啟動系統 (Running the System)

請使用以下指令編譯並執行應用程式：

```powershell
# 切換到 ConsoleUI 目錄
cd c:\Projects\stock\YuantaOneAPI_CSharp\AiStockAdvisor\AiStockAdvisor.ConsoleUI

# 編譯並執行
dotnet run
```

*注意：由於 `YuantaOneAPI` 依賴 .NET Framework 4.8，請確保您的 Windows 已安裝該版本（通常 Windows 10/11 內建）。*

## 3. 預期結果 (Expected Output)

執行 `dotnet run` 後，程式會要求您輸入帳號密碼：

```text
Starting AiStockAdvisor...
Enter Yuanta Account (ID): [輸入您的帳號]
Enter Password: [輸入您的密碼]
```

輸入完畢後，您應該會看到以下 Log：

1.  **系統啟動**: `[INF] Logger initialized...`
2.  **連線登入**: 
    *   `[INF] [YuantaBrokerClient] Connecting to Yuanta API (UAT Framework)...`
    *   `[INF] [YuantaBrokerClient] logging in as User: XXXXX...`
    *   `[INF] [YuantaBrokerClient] Login successfully initiated.`
3.  **訂閱股票**: `[INF] [YuantaBrokerClient] Subscribing to 2327...` (預設訂閱國巨)。
4.  **接收數據**:
    *   當市場有交易時，您會看到类似 Log:
        ```text
        [YuantaBrokerClient] Parsed Tick: [10:30:05] 2330 @ 550.00 (Vol: 5)
        ```
    *   **策略訊號**: 當每分鐘結束時，K 線生成器會觸發策略：
        ```text
        [MA Cross Strategy] Received Bar: [10:31:00] 2330 Open:550 High:551 Low:549 Close:550 Vol:105
        [MA Cross Strategy] MA2: 550.00, MA5: 548.00
        [MA Cross Strategy] Signal: BULLISH
        ```

5.  **日誌檔案 (Logs)**:
    *   系統使用 **Serilog** 進行記錄，自動支援檔案滾動 (Rolling) 與保留策略 (30 天)。
    *   儲存位置：執行目錄下的 `Logs` 資料夾。
    *   檔名格式：`log-yyyyMMdd.txt` (例如 `log-20231223.txt`)。
    *   內容包含精確時間戳記、層級 (INFO/WARN/ERROR) 與詳細訊息。

## 4. 常見問題排除 (Troubleshooting)

*   **Error: DllNotFoundException (YuantaCAPIDLL64.dll)**:
    *   原因：找不到原生的 C++ DLL。
    *   解法：請確認 `bin\Debug\net48\` 目錄下是否有 `YuantaCAPIDLL64.dll`。如果沒有，請手動從專案根目錄複製過去。

*   **Error: BadImageFormatException**:
    *   原因：位元版本不符 (32-bit vs 64-bit)。
    *   解法：本專案預設使用 64-bit DLL。請確保您的專案設定 (Platform Target) 為 `x64` 或 `Any CPU` (且取消勾選 Prefer 32-bit)。

*   **無數據 (No Data)**:
    *   原因：現在可能是非盤中時間，或訂閱失敗。
    *   解法：請在台股盤中時間 (09:00 - 13:30) 測試，或確認 API 是否連接到模擬主機。
