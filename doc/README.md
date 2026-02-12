# 文件入口

```text
+--------------+    +------------+    +------------+
| architecture | -> | messaging  | -> | data       |
+--------------+    +------------+    +------------+
        |                 |                 |
        v                 v                 v
+--------------+    +------------+    +------------+
| plans        |    | uat        |    | features   |
+--------------+    +------------+    +------------+
        |
        v
+--------------+
| archive      |
+--------------+
```

## Read First

1. `architecture/README.md`
2. `messaging/README.md`
3. `data/README.md`
4. `plans/README.md`
5. `uat/README.md`

## 目錄地圖

- `architecture/`: 系統與日誌架構
- `messaging/`: RabbitMQ 拓撲、權限、CLI、事故修正
- `data/`: DDL、Timescale migration、tick payload 規格
- `plans/`: 長篇開發規劃文件
- `uat/`: UAT 分階段文件（phase-1/phase-2）
- `features/`: feature 級開發與 review 文件
- `external/`: 外部工具與部署素材（BaGet）
- `archive/`: 歷史報告與封存資料

## 命名規範

- Markdown 一律 `kebab-case`
- 分階段文件格式：`<topic>-phase-<n>-<scope>.md`
- 不再使用 `v1/v2` 命名表示階段

## 維護規則

- 新架構文件需先放 ASCII 概覽圖（<=9 節點）
- Mermaid 圖維持小圖：Container <=12、Component <=15
- 同主題跨資料夾時，連結用 repo 絕對路徑：`/doc/...`
