# ProductApi

一個以 **ASP.NET Core Minimal API** 打造的商品管理 RESTful API,使用 **Entity Framework Core + SQLite** 作為資料儲存,並示範了相依性注入(DI)、非同步資料存取,以及透過 Endpoint Filter 做模型驗證。

## 技術棧

| 項目 | 說明 |
|------|------|
| .NET | net10.0 |
| Web 框架 | ASP.NET Core Minimal API |
| ORM | Entity Framework Core 10.0.9 |
| 資料庫 | SQLite（`app.db`，本機檔案） |
| 驗證 | `System.ComponentModel.DataAnnotations` + Endpoint Filter |

## 專案結構

```
ProductApi/
├── Program.cs           # 進入點：DI 註冊、資料庫初始化、路由與驗證
├── ProductService.cs    # Product 模型、IProductService 介面與實作（商務邏輯）
├── AppDbContext.cs      # EF Core DbContext，對應 Products 資料表
├── appsettings.json     # 應用程式設定
├── test.http            # API 手動測試腳本（VS Code REST Client）
└── app.db*              # SQLite 資料庫檔案（本機產生，已 gitignore）
```

## 快速開始

### 需求
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### 執行

```bash
# 還原套件並執行
dotnet restore
dotnet run
```

啟動後服務會監聽於 `http://localhost:5226`（見 `Properties/launchSettings.json`）。

首次啟動時,若 `app.db` 不存在會**自動建立資料庫**,並塞入 3 筆種子商品資料。

## 資料模型

```jsonc
{
  "id": 1,               // 主鍵，由資料庫自動遞增產生（新增時不需提供）
  "name": "高階人體工學椅", // 必填，長度 2~100
  "price": 8800,          // 必填，範圍 1 ~ 100,000
  "stock": 10             // 必填，不可為負數（0 以上）
}
```

## API 端點

| 方法 | 路徑 | 說明 | 成功回應 |
|------|------|------|----------|
| GET | `/api/products` | 取得所有商品 | `200 OK` |
| GET | `/api/products/{id}` | 依 ID 取得單一商品 | `200 OK` / `404 Not Found` |
| POST | `/api/products` | 新增商品 | `201 Created` / `400 Bad Request` |

### 範例:新增商品

> 注意:新增時**不需要**傳入 `id`,主鍵由資料庫自動產生。

```http
POST http://localhost:5226/api/products
Content-Type: application/json

{
    "name": "4K螢幕",
    "price": 15000,
    "stock": 5
}
```

驗證失敗（例如價格為負）時,會回傳 `400` 與錯誤明細:

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Price": ["商品價格必須在1元到100,000之間"]
  }
}
```

更多測試案例可直接開啟 `test.http`（搭配 VS Code 的 REST Client 擴充套件執行）。

## 說明:SQLite 的 WAL 模式

EF Core 對 SQLite 預設啟用 **WAL（Write-Ahead Logging）**,新寫入的資料會先進 `app.db-wal`,尚未合併回主檔 `app.db`。

- 資料並未遺失 — 程式讀取時 SQLite 會自動合併 `app.db` 與 `app.db-wal`。
- 若用 DB 檢視工具只開 `app.db` 而未讀 `-wal`,可能只看到舊資料。
- 乾淨關閉程式,或執行 `PRAGMA wal_checkpoint(TRUNCATE);` 即可將資料合併回主檔。

## 授權

本專案僅供學習與示範用途。
