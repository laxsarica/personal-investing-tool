# PROJECT.md — ScreenEdge

> This file provides project context for AI assistants working on this codebase.

---

## Project Identity

| Field | Value |
|---|---|
| **Application Name** | ScreenEdge |
| **Type** | Indian Stock Market Technical Screener Platform |
| **Target Market** | NSE / BSE (Indian Equities) |
| **Origin** | Extracted from MarketWeave (NebulaNest) |
| **Scope** | Technical Screeners ONLY (no broker, no IFM, no data ingestion) |

---

## What This Project Does

This is an **automated technical screening system** for Indian stock markets. It:

1. **Scans** all NSE equity stocks against multiple technical analysis strategies
2. **Detects** buy signals using proprietary custom indicators
3. **Filters** results by RSI band (55-70) to target healthy uptrends
4. **Persists** screener results to a database for tracking and analysis
5. **Automates** the entire process via scheduled background jobs

---

## Solution Architecture

```
ScreenEdge.sln
│
├── Ta.Indicator                    ← Core technical indicators library
│   ├── Base/                       ← Base models (PriceHistory, Result, TimeSeriesData)
│   ├── BaseFunction/               ← Utilities (DataConverter, CandleStick, IndicatorBase)
│   └── Indicator/                  ← ADX, AMA, ATR, EMA, KAMA, LINEARREG, RSI, SMA, VolumeMA, WMA
│
├── Ta.CustomIndicator              ← Custom composite indicators
│   ├── BreakOut/                   ← Support/Resistance breakout detection
│   ├── EmaFifty/                   ← EMA-50 crossover with volume confirmation
│   ├── ExitOscillator/             ← Chandelier Exit oscillator
│   ├── Rsima/                      ← RSI-weighted moving average
│   └── ZeroLag/                    ← Zero-lag EMA signal detection
│
├── ScreenEdge.Entity               ← EF Core DbContext + entities
├── ScreenEdge.Repository           ← Repository + Unit of Work
├── ScreenEdge.Screener             ← Screener engine orchestrator
├── ScreenEdge.Api                  ← ASP.NET Core API entry point
├── ScreenEdge.Web                  ← Angular 20 SPA (desktop-only, data-dense)
│   └── ClientApp/                  ← Angular project root
└── ScreenEdge.Tests                ← Unit tests (xUnit)
```

---

## Technical Screeners (4 Active Strategies)

### 1. ZeroLag Screener (`NOLAG`)
- **Timeframes:** Daily + Weekly
- **Signal:** ZeroLag EMA crosses from below to above regular EMA
- **Filter:** RSI > 55 (entry condition) AND RSI 55-70 (final filter)
- **Indicator:** `ZeroLagIndicator` (Length=15, AtrLength=16)

### 2. EMA-50 Crossover Screener (`EMAFIFTY`)
- **Timeframes:** Daily + Weekly
- **Signal:** Price crosses above 50-period EMA with 20% volume spike
- **Filter:** RSI > 55 AND RSI 55-70
- **Indicator:** `EmaFiftyIndicator` (EmaLength=50, VolumeLength=20)

### 3. Support/Resistance Breakout Screener (`SUPPORTRESISTANCE`)
- **Timeframes:** Daily only
- **Signal:** Resistance breakout detected within last 2 trading days
- **Filter:** RSI > 54
- **Indicator:** `SupportResistanceBreakOutIndicator` (lookback=20)

### 4. RSI-Weighted Moving Average Screener (`RSIWMA`)
- **Timeframes:** Daily only
- **Signal:** Price crosses above RSI-weighted MA (bullish cross)
- **Filter:** RSI > 55 AND RSI 55-70
- **Indicator:** `RsiWeightedMovingAverageIndicator` (RsiPeriod=14, MaPeriod=55)

---

## Tech Stack

### Backend
- **.NET 8.0** (LTS)
- **ASP.NET Core Minimal API**
- **Entity Framework Core 9** — SQL Server
- **SQL Server (LocalDB)**
- **Swagger / OpenAPI**

### Libraries
- **LumenWorks.Framework.IO** — CSV parsing (used by Ta.Indicator)
- **Newtonsoft.Json** — JSON serialization (used by Ta.Indicator)

### Frontend
- **Angular 20** — standalone components, no NgModules
- **TypeScript 5.8** — strict mode
- **Desktop-only**, light theme, data-dense, table-first layout
- **Design system:** Tickertape-inspired density — data IS the interface
- **Fonts:** Inter (sans, UI labels) + JetBrains Mono (mono, all numeric data)
- **Color palette:** Muted slate-blue accent (`#2B4C7E`), gain green (`#158443`), loss red (`#C4291C`)
- **Key rule:** No animations, no card layouts for data, no pills/badges — hairline borders and colored text only
- **No PrimeNG or heavy UI libraries** — vanilla CSS with design tokens

### Automation
- Scheduled background jobs (Hangfire or Quartz.NET)
- Parallel execution (up to 10 concurrent stock scans)

---

## Key Domain Concepts

| Term | Meaning |
|------|---------|
| **NSE** | National Stock Exchange of India |
| **RSI** | Relative Strength Index — momentum oscillator (0-100) |
| **EMA** | Exponential Moving Average |
| **SMA** | Simple Moving Average |
| **ATR** | Average True Range — volatility measure |
| **ADX** | Average Directional Index — trend strength |
| **KAMA** | Kaufman Adaptive Moving Average |
| **ZeroLag** | Zero-lag smoothed indicator — reduces EMA delay |
| **S/R** | Support / Resistance price levels |
| **OHLCV** | Open, High, Low, Close, Volume — standard bar data |
| **Bullish Cross** | Price crossing above an indicator line |
| **Bearish Cross** | Price crossing below an indicator line |

---

## Development Conventions

### Code Style
- File-scoped namespaces: `namespace X;`
- Top-level statements in `Program.cs`
- Async/await for all database operations
- Primary constructors where appropriate

### Naming
- Projects: `PascalCase`
- Entities: Singular (`TickerHistory`, not `TickerHistories`)
- DbSets: Plural (`TickerHistories`)
- Interfaces: `I`-prefix

### Architecture Patterns
- Repository + Unit of Work
- Manager pattern for business logic
- Facade pattern for multi-implementation dispatch
- Feature-based folder structure for frontend

---

## Common Commands

```bash
# Restore & build
dotnet restore ScreenEdge.sln
dotnet build ScreenEdge.sln

# Run the API
dotnet run --project ScreenEdge.Api

# Run tests
dotnet test ScreenEdge.Tests/ScreenEdge.Tests.csproj

# EF Core migrations
dotnet ef migrations add <Name> --project ScreenEdge.Entity --startup-project ScreenEdge.Api
dotnet ef database update --project ScreenEdge.Entity --startup-project ScreenEdge.Api
```

---

## File Quick Reference

| What you need | Where to find it |
|---|---|
| Core indicator base types | `Ta.Indicator/Base/` |
| Data conversion utilities | `Ta.Indicator/BaseFunction/DataConverter.cs` |
| Candlestick patterns | `Ta.Indicator/BaseFunction/CandleStickPattern.cs` |
| RSI, EMA, SMA, ATR, ADX | `Ta.Indicator/Indicator/` |
| ZeroLag indicator | `Ta.CustomIndicator/ZeroLag/` |
| EMA-50 crossover | `Ta.CustomIndicator/EmaFifty/` |
| S/R breakout detection | `Ta.CustomIndicator/BreakOut/` |
| RSI-Weighted MA | `Ta.CustomIndicator/Rsima/` |
| Chandelier Exit | `Ta.CustomIndicator/ExitOscillator/` |
| Screener orchestrator | `ScreenEdge.Screener/ScreenerEngine.cs` |
| Original source reference | `C:\Users\vivek\source\repos\MarketWeave\MarketWeave\` |
