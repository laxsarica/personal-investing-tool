# MyStockMarketTool — Agent Rules

## Project Identity
- **Application Name:** MyStockMarketTool
- **Type:** Indian Stock Market Technical Screener Platform
- **Target Market:** NSE / BSE (Indian Equities)
- **Origin:** Extracted from MarketWeave (NebulaNest) — Technical Screeners module only

## Tech Stack Rules
- **Backend:** .NET 8.0 (LTS), ASP.NET Core Minimal API, Entity Framework Core 9, SQL Server LocalDB
- **Frontend:** Angular 20 (standalone components, no NgModules). Desktop-only, light theme, data-dense table-first UI. Inter + JetBrains Mono fonts. Zone.js change detection (default) — no signals, no OnPush, no manual ChangeDetectorRef. See `ui-design-system` skill for full spec.
- **All projects** must target `net8.0`
- **Use file-scoped namespaces** — `namespace X;` (not `namespace X { }`)
- **Use top-level statements** in `Program.cs`
- **All DB operations must be async** — use async/await throughout
- **Use the Repository + Unit of Work pattern** for data access

## Code Style
- C# naming: PascalCase for projects, types, methods. Singular nouns for entities, plural for DbSets.
- Interfaces use `I`-prefix (`IScreenerEngine`, `IBaseRepository<T>`)
- Angular/React components: kebab-case files
- Feature-based folder structure for frontend

## Architecture Patterns
- **Repository + Unit of Work** — data access abstraction
- **Manager Pattern** — business logic in `*Manager` classes, not in controllers
- **Facade Pattern** — when dispatching to multiple concrete implementations
- **Standalone components** — no heavy module systems

## Domain Vocabulary
| Term | Meaning |
|------|---------|
| **NSE** | National Stock Exchange of India |
| **RSI** | Relative Strength Index |
| **EMA** | Exponential Moving Average |
| **SMA** | Simple Moving Average |
| **ATR** | Average True Range |
| **ADX** | Average Directional Index |
| **KAMA** | Kaufman Adaptive Moving Average |
| **WMA** | Weighted Moving Average |
| **DMA** | Daily Moving Average |
| **ZeroLag** | Zero-lag smoothed indicator |
| **S/R** | Support / Resistance levels |

## Testing Rules
- All indicator calculations must have unit tests verifying accuracy
- Use xUnit for .NET testing
- Test edge cases: insufficient data, empty lists, single-element lists

## Error Handling
- Never silently swallow exceptions in production code
- Log errors with structured logging (Serilog or built-in ILogger)
- Screener methods should return empty results on failure, not throw

## Important Notes
- This project is a FRESH extraction from MarketWeave — do NOT import MarketWeave.Broker, MarketWeave.InstitutionalFlowMomentum, or MarketWeave.StockDataIngestion
- The Technical Analysis libraries (Ta.Indicator, Ta.CustomIndicator) are the core IP — preserve their calculation logic exactly
- The ScreenerEngine is being refactored to be independent of the original AppDbContext
