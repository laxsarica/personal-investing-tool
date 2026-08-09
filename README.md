# MyStockMarketTool — Personal Investing Tool

A personal **Indian stock market technical screener platform** for NSE/BSE equities. Built as a full-stack application with a .NET 8 backend and Angular 20 frontend.

---

## 🏗️ Tech Stack

| Layer | Technology |
|---|---|
| **Backend API** | .NET 8, ASP.NET Core Minimal API |
| **ORM** | Entity Framework Core 9, SQL Server LocalDB |
| **Frontend** | Angular 20 (standalone components) |
| **Broker Integration** | Angel One SmartAPI, Zerodha Kite |
| **Fonts** | Inter, JetBrains Mono |

---

## 📁 Project Structure

```
MyStockMarketTool/
├── ScreenEdge.Api/          # ASP.NET Core Web API (entry point)
│   ├── Controllers/         # ScreenerController, KiteAuthController
│   ├── Services/            # DataIngestionService (Angel One data sync)
│   └── Program.cs           # DI setup, middleware, Quartz scheduler
│
├── ScreenEdge.Entity/       # EF Core entities & DbContext
│   ├── Entities/            # DistinctStock, TickerHistory, Screener
│   └── Migrations/          # EF Core migration history
│
├── ScreenEdge.Repository/   # Repository + Unit of Work pattern
│
├── ScreenEdge.Screener/     # Core screener engine
│   └── ScreenerEngine.cs    # Parallel screener job runner
│
├── ScreenEdge.Broker/       # Broker abstraction layer
│   ├── IBrokerPortfolioProvider.cs
│   ├── AngelOnePortfolioProvider.cs
│   └── KitePortfolioProvider.cs
│
├── Ta.Indicator/            # Core technical indicator library
│   └── Indicator/           # RSI, EMA, SMA, ATR, ADX, KAMA, WMA, etc.
│
├── Ta.CustomIndicator/      # Composite screener indicators
│   ├── ZeroLag/             # ZeroLag EMA crossover
│   ├── EmaFifty/            # EMA 50 breakout
│   ├── BreakOut/            # Support/Resistance breakout
│   └── Rsima/               # RSI Weighted Moving Average
│
├── ScreenEdge.Tests/        # xUnit test project
│
└── ScreenEdge.Web/          # Angular 20 frontend
    └── ClientApp/
        └── src/app/
            ├── features/screener/   # Screener dashboard, history, jobs
            ├── features/portfolio/  # Portfolio view (Angel One + Kite)
            └── features/auth/       # Login page
```

---

## 🔍 Screener Strategies

| Strategy | Description |
|---|---|
| **NOLAG** | Zero-lag EMA crossover signal |
| **EMAFIFTY** | Price breakout above EMA 50 |
| **SUPPORTRESISTANCE** | Support/Resistance level breakout |
| **RSIFULL** | RSI + Weighted Moving Average signal |

Each strategy runs on both **Daily (D)** and **Weekly (W)** timeframes.

---

## 📊 Key Features

- **Parallel screener engine** — scans all NSE equities concurrently
- **Multi-timeframe detection** — highlights stocks appearing in both D & W timeframes with an orange `D+W` badge
- **Multi-strategy detection** — highlights stocks detected by multiple strategies with a blue ★ badge
- **RSI filters** — filter results by Daily / Weekly / Monthly RSI range with Low / Mid / High presets
- **TotalTradingDays optimisation** — new IPOs (< 21 trading days) are skipped automatically
- **Dual broker support** — Angel One SmartAPI and Zerodha Kite for portfolio data
- **Quartz.NET scheduler** — automated screener jobs running in the background

---

## ⚙️ Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (comes with Visual Studio)
- [Angular CLI 20](https://angular.dev/): `npm install -g @angular/cli`

---

## 🚀 Getting Started

### 1. Clone the repository
```bash
git clone https://github.com/laxsarica/personal-investing-tool.git
cd personal-investing-tool
```

### 2. Configure appsettings
Edit `ScreenEdge.Api/appsettings.Development.json` with your credentials:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ScreenEdgeDb;Trusted_Connection=True;"
  },
  "BrokerSettings": {
    "AngelOne": {
      "ApiKey": "YOUR_ANGEL_ONE_API_KEY",
      "ClientId": "YOUR_CLIENT_ID",
      "Password": "YOUR_PASSWORD",
      "Totp": "YOUR_TOTP_SECRET"
    },
    "Kite": {
      "ApiKey": "YOUR_KITE_API_KEY",
      "ApiSecret": "YOUR_KITE_API_SECRET"
    }
  }
}
```

### 3. Apply database migrations
```bash
dotnet ef database update --project ScreenEdge.Entity --startup-project ScreenEdge.Api
```

### 4. Run the API
```bash
dotnet run --project ScreenEdge.Api
# API runs at: http://localhost:5000
# Swagger UI:  http://localhost:5000/swagger
```

### 5. Run the Angular frontend
```bash
cd ScreenEdge.Web/ClientApp
npm install
ng serve
# App runs at: http://localhost:4200
```

---

## 📡 Key API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/Screener/run-job` | Trigger screener manually |
| `POST` | `/api/Screener/sync-daily` | Sync today's price data |
| `POST` | `/api/Screener/sync-historical` | Sync full historical data for a symbol |
| `POST` | `/api/Screener/sync-all-historical` | Sync all NSE equities (long running) |
| `GET` | `/api/Screener/results` | Get latest screener results |
| `GET` | `/api/Screener/results/history` | Get historical results with pagination |
| `GET` | `/api/Kite/login-url` | Get Kite OAuth login URL |
| `GET` | `/api/Portfolio/holdings` | Get holdings from connected broker |

---

## 📐 Architecture Patterns

- **Repository + Unit of Work** — all DB access through `IBaseRepository<T>` and `IUnitOfWorks`
- **Manager Pattern** — business logic isolated in service classes
- **Facade / Strategy Pattern** — `IBrokerPortfolioProvider` abstracts Angel One and Kite
- **Parallel execution** — `Parallel.ForEachAsync` with scoped DI for thread-safe EF Core

---

## 🧪 Running Tests

```bash
dotnet test ScreenEdge.Tests
```

Tests cover indicator calculation accuracy and screener engine logic.

---

## 📝 Notes

- The **Technical Analysis libraries** (`Ta.Indicator`, `Ta.CustomIndicator`) are the core IP — calculation logic is preserved exactly from original MarketWeave implementation
- Angel One provides up to **5 years** of historical OHLCV data (weekly candles)
- `TotalTradingDays` column on `DistinctStocks` tracks how many trading sessions of data exist per symbol, enabling efficient IPO filtering
