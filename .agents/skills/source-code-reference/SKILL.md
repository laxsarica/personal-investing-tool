---
name: source-code-reference
description: Skill containing the original source code reference from the MarketWeave project. This is the authoritative reference for migrating the Technical Screener codebase. All indicator calculations, data models, and screener logic are documented here with their exact original source paths.
---

# Source Code Reference Skill

## Purpose
This skill serves as the authoritative reference map for migrating code from the original MarketWeave project. It lists every source file that needs to be migrated, its original location, and what needs to change during migration.

## Original Project Location
`C:\Users\vivek\source\repos\MarketWeave\MarketWeave\`

## File Inventory

### Ta.Indicator Project (Core Library)

#### Base Types (`Ta.Indicator/Base/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `PriceHistory.cs` | 13 | ✅ As-is | Core OHLCV model |
| `TimeSeriesData.cs` | 14 | ✅ As-is | Indicator output model |
| `Result.cs` | 14 | ✅ As-is | Standard indicator return type |
| `Candle.cs` | 10 | ✅ As-is | Simplified OHLC for patterns |
| `ColumnType.cs` | 13 | ✅ As-is | Enum for price column selection |
| `CrossType.cs` | 8 | ✅ As-is | Bullish/Bearish/None enum |
| `Rootobject.cs` | 10 | ✅ As-is | JSON deserialization model |

#### Base Functions (`Ta.Indicator/BaseFunction/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `IndicatorCalculatorBase.cs` | 26 | ✅ As-is | Abstract base for indicators |
| `DataConverter.cs` | 167 | ✅ As-is | CSV/JSON/Weekly/Monthly converters |
| `CandleStickPattern.cs` | 157 | ✅ As-is | Doji, Hammer, Engulfing, etc. |
| `CandleStickConverstion.cs` | 32 | ✅ As-is | Heikin Ashi converter |

#### Indicators (`Ta.Indicator/Indicator/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `RSI.cs` | 62 | ✅ As-is | Wilder RSI |
| `EMA.cs` | 47 | ✅ As-is | Exponential MA |
| `SMA.cs` | 37 | ✅ As-is | Simple MA |
| `ATR.cs` | 76 | ✅ As-is | Average True Range |
| `ADX.cs` | 125 | ✅ As-is | Average Directional Index |
| `AMA.cs` | ~50 | ✅ As-is | Adaptive MA |
| `KAMA.cs` | ~75 | ✅ As-is | Kaufman Adaptive MA |
| `WMA.cs` | ~40 | ✅ As-is | Weighted MA |
| `VolumeMA.cs` | 41 | ✅ As-is | Volume MA (note different namespace) |
| `LINEARREG.cs` | ~75 | ✅ As-is | Linear Regression |

#### Project File
| File | Notes |
|------|-------|
| `Ta.Indicator.csproj` | Target: net8.0. Deps: LumenWorks.Framework.IO, Newtonsoft.Json |

---

### Ta.CustomIndicator Project (Custom Composites)

#### ZeroLag (`Ta.CustomIndicator/ZeroLag/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `ZeroLagIndicator.cs` | 82 | ✅ As-is | Zero-lag EMA signal detector |
| `ZeroLagResult.cs` | 11 | ✅ As-is | Date + UpSignal + DownSignal |

#### EmaFifty (`Ta.CustomIndicator/EmaFifty/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `EmaFiftyIndicator.cs` | 62 | ✅ As-is | EMA-50 crossover with volume confirmation |
| `EmaFiftyResult.cs` | 15 | ✅ As-is | Date + UpSignal |

#### BreakOut (`Ta.CustomIndicator/BreakOut/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `SupportResistanceBreakOutIndicator.cs` | 301 | ✅ As-is | S/R detection + breakout signals |
| `SupportResistanceResult.cs` | 9 | ✅ As-is | Date + UpSignal + DownSignal |
| `InternalResult.cs` | 16 | ✅ As-is | Internal arrays for S/R levels |
| `BaseCodeFromGpt.cs` | ~180 | ⚠️ Review | GPT-generated code — may be legacy/unused |

#### Rsima (`Ta.CustomIndicator/Rsima/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `RsiMovingAverageIndicator.cs` | 137 | ✅ As-is | RSI-weighted MA with cross detection |
| `RsiMovingAvarageResult.cs` | 32 | ✅ As-is | Full result model with BullishCross/BearishCross |

#### ExitOscillator (`Ta.CustomIndicator/ExitOscillator/`)
| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `Chandelierexitoscillator.cs` | 116 | ✅ As-is | Chandelier Exit + normalized oscillator |

#### Project File
| File | Notes |
|------|-------|
| `Ta.CustomIndicator.csproj` | Target: net8.0. Deps: Ta.Indicator |

---

### MarketWeave.Screener Project (Orchestrator)

| File | Lines | Migrate? | Notes |
|------|-------|----------|-------|
| `ScreenerEngine.cs` | 248 | ⚠️ Refactor | Remove MarketWeave.Broker dependency, fix thread safety, use DI properly |

#### Project File
| File | Notes |
|------|-------|
| `MarketWeave.Screeners.csproj` | Target: net8.0. Has 6 project references — reduce to only Ta.Indicator + Ta.CustomIndicator + new Entity/Repository |

---

## Migration Notes

### Files to Copy Verbatim (No Changes)
All files in `Ta.Indicator/` and `Ta.CustomIndicator/` should be copied exactly as-is. The indicator calculation logic is proven and tested — do not modify it.

### Files Requiring Refactoring
1. **ScreenerEngine.cs** — Major refactoring needed:
   - Remove `using Ta.Broker;` (exchange master data)
   - Remove `using MarketWeave.Entity.Entities;` → create new entity project
   - Remove `using MarketWeave.Models.Enums;` → create local StrategyEnum
   - Fix thread-safety (ConcurrentBag instead of List)
   - Fix shared mutable state (RsiDaily/RsiWeekly fields)
   - Use `IDbContextFactory` instead of `new AppDbContext()`

### Dependencies to Drop
- `MarketWeave.Broker` — Not needed for screener
- `MarketWeave.Models` — Only need StrategyEnum, recreate locally
- `MarketWeave.Entity` — Create new, minimal entity project
- `MarketWeave.Repository` — Create new, minimal repository

### New Files to Create
1. New `AppDbContext` with only screener-relevant DbSets
2. New `IUnitOfWorks` with only screener-relevant repositories
3. New `StrategyEnum` (local to screener project)
4. New API project (entry point)
5. New frontend project (TBD)
