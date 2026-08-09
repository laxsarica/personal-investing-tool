# IIS Deployment + Hangfire Background Jobs

## Overview
Two goals:
1. **Replace `dotnet run`** with proper Local IIS hosting for both the .NET API and Angular frontend
2. **Add Hangfire** to replace the current manual-trigger screener with a proper scheduled background job with a dashboard UI

---

## Part 1 — Hangfire Integration

### Why Hangfire over Quartz?
The current setup has no scheduler — screener runs are triggered manually via API. Hangfire gives us:
- **Dashboard UI** at `/hangfire` to see job history, failures, retries
- **Recurring jobs** (cron-based daily sync + screener run)
- **Retry on failure** automatically
- **SQL Server storage** (reuses the existing LocalDB connection)

### Changes Required

#### [MODIFY] [ScreenEdge.Api.csproj](file:///d:/MyStockMarketTool/ScreenEdge.Api/ScreenEdge.Api.csproj)
Add NuGet packages:
- `Hangfire.Core`
- `Hangfire.SqlServer`
- `Hangfire.AspNetCore`

#### [MODIFY] [Program.cs](file:///d:/MyStockMarketTool/ScreenEdge.Api/Program.cs)
- Register `AddHangfire()` with SQL Server storage (same connection string)
- Register `AddHangfireServer()`
- Map `UseHangfireDashboard("/hangfire")`
- After `app.Run()` setup, register recurring jobs:
  - **Daily sync** — every weekday at 6:30 AM IST (`0 1 * * 1-5` UTC)
  - **Run screener** — every weekday at 7:00 AM IST (`30 1 * * 1-5` UTC)

#### [NEW] `ScreenEdge.Api/Jobs/ScreenerJob.cs`
Hangfire-friendly job class wrapping `DataIngestionService.SyncDailyDataAsync()` and `ScreenerEngine.RunScreenerJobAsync()`.

#### [MODIFY] `ScreenEdge.Api/appsettings.json`
Add CORS origin for IIS URLs.

---

## Part 2 — Local IIS Deployment

### Prerequisites (one-time manual steps)

> [!IMPORTANT]
> These steps require manual installation before I can configure the project files.

1. **Enable IIS** in Windows:
   - Open "Turn Windows Features On or Off"
   - Enable: Internet Information Services → Web Management Tools → IIS Management Console
   - Enable: Internet Information Services → World Wide Web Services → Application Development Features → **CGI**, **ISAPI Extensions**, **ISAPI Filters**

2. **Install ASP.NET Core Hosting Bundle** (for .NET 8):
   - Download from: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
   - Install the **"ASP.NET Core Runtime — Hosting Bundle"** (not just the runtime)
   - Restart IIS after install: run `iisreset` in Admin PowerShell

### Architecture on IIS

```
IIS
├── screenedge-api    → http://localhost:5100   (ASP.NET Core App)
│   └── Publishes to: D:\IIS\ScreenEdge.Api\
│
└── screenedge-web    → http://localhost:4200   (Static Files - Angular build)
    └── Publishes to: D:\IIS\ScreenEdge.Web\
```

### Changes Required

#### [NEW] `ScreenEdge.Api/web.config`
ASP.NET Core IIS integration — required for IIS to forward requests to the Kestrel process.

#### [NEW] `ScreenEdge.Web/ClientApp/web.config`
URL rewrite rules so Angular routing works correctly (redirect all 404s to `index.html`).

#### [MODIFY] `ScreenEdge.Api/appsettings.json`
Update CORS to allow IIS-hosted Angular URL.

---

## Proposed Job Schedule

| Job | Cron | IST Time | Description |
|---|---|---|---|
| `SyncDailyData` | `0 1 * * 1-5` | 6:30 AM Mon–Fri | Fetch today's OHLCV data |
| `RunScreener` | `30 1 * * 1-5` | 7:00 AM Mon–Fri | Run all 4 screeners |

---

## Verification Plan

### Automated
- Build passes: `dotnet build`
- Publish succeeds: `dotnet publish`

### Manual
1. Open `http://localhost:5100/swagger` — API Swagger UI loads
2. Open `http://localhost:5100/hangfire` — Hangfire dashboard loads, shows recurring jobs
3. Open `http://localhost:4200` — Angular app loads and connects to API
4. Trigger a screener run from Hangfire dashboard manually — verify results appear in Angular
