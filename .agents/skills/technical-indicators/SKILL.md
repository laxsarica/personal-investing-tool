---
name: technical-indicators
description: Skill for understanding, using, and extending the Technical Analysis indicator library (Ta.Indicator and Ta.CustomIndicator). Covers core indicators (RSI, EMA, SMA, ATR, ADX, KAMA, WMA, VolumeMA, LINEARREG, AMA) and custom composite indicators (ZeroLag, EmaFifty, SupportResistanceBreakOut, RsiWeightedMovingAverage, ChandelierExitOscillator). Also covers candlestick patterns, data conversion utilities, and the Heikin Ashi converter.
---

# Technical Indicators Skill

## Overview
This skill covers the complete technical analysis library extracted from the MarketWeave project. It consists of two .NET class libraries:

1. **Ta.Indicator** — Core indicator calculations and base abstractions
2. **Ta.CustomIndicator** — Composite/custom indicators that build on the core library

## Architecture

### Base Abstractions (`Ta.Indicator/Base/`)

| File | Type | Purpose |
|------|------|---------|
| `PriceHistory.cs` | Class | OHLCV bar data model: `Date`, `Open`, `High`, `Low`, `Close`, `Volume` (all `double`) |
| `TimeSeriesData.cs` | Class | Single indicator output point: `Value` (`double?`), `DateTime` |
| `Result.cs` | Class | Wraps `List<TimeSeriesData> ResultData` — standard return type for all core indicators |
| `Candle.cs` | Class | Simplified OHLC model (no Date/Volume) for candlestick pattern recognition |
| `ColumnType.cs` | Enum | `Open, High, Low, Close, Volume, AdjClose` — specifies which price column to use |
| `CrossType.cs` | Enum | `None, Bullish, Bearish` — crossover direction |
| `Rootobject.cs` | Class | JSON deserialization model for external data (`status`, `message`, `errorcode`, `data[][]`) |

### Base Functions (`Ta.Indicator/BaseFunction/`)

| File | Purpose |
|------|---------|
| `IndicatorCalculatorBase<T>` | Abstract base class. Requires `PriceHistoryList` property and `Calculate()` method. Includes `Load()` for CSV/JSON file input. |
| `DataConverter` | Static utilities: `ConvertFromCsv`, `ConvertFromJson`, `ConvertFromString`, `ConvertToWeeklyOHLC`, `ConvertToMonthlyOHLC` |
| `CandleStickPattern` | Static pattern detection: Doji, Hammer, InvertedHammer, BullishEngulfing, PiercingPattern, BullishHarami, BullishHaramiCross, TweezerBottom, MorningStar |
| `CandleStickConverstion` | Static `ToHeikinAshi()` converter |

### Core Indicators (`Ta.Indicator/Indicator/`)

All inherit from `IndicatorCalculatorBase<Result>` and return a `Result` object.

| Indicator | Constructor | Namespace | Key Logic |
|-----------|-------------|-----------|-----------|
| `RSI` | `RSI(int period)` | `TA.Indicators.Indicator` | Wilder's smoothed RSI calculation |
| `EMA` | `EMA(int period)` | `TA.Indicators.Indicator` | Exponential MA with SMA seed for warmup |
| `SMA` | `SMA(int period)` | `TA.Indicators.Indicator` | Simple moving average |
| `ATR` | `ATR(int period)` | `TA.Indicators.Indicator` | Average True Range (Wilder smoothing) |
| `ADX` | `ADX(int period)` | `TA.Indicators.Indicator` | Average Directional Index using ATR internally |
| `AMA` | `AMA(int period)` | `TA.Indicators.Indicator` | Adaptive Moving Average |
| `KAMA` | `KAMA(int period)` | `TA.Indicators.Indicator` | Kaufman Adaptive Moving Average |
| `WMA` | `WMA(int period)` | `TA.Indicators.Indicator` | Weighted Moving Average |
| `VolumeMA` | `VolumeMA(int period)` | `Ta.Indicator.Indicator` | Volume Moving Average (note: different namespace) |
| `LINEARREG` | `LINEARREG(int period)` | `TA.Indicators.Indicator` | Linear Regression |

> **IMPORTANT NAMESPACE NOTE:** Most indicators use `TA.Indicators.Indicator` (capital TA, plural Indicators), but `VolumeMA` uses `Ta.Indicator.Indicator` (mixed case, singular). This inconsistency exists in the original codebase and must be preserved during migration.

### Custom Indicators (`Ta.CustomIndicator/`)

These are standalone classes (not inheriting from `IndicatorCalculatorBase`) that take `List<PriceHistory>` directly.

#### ZeroLag Indicator (`ZeroLag/`)
- **Class:** `ZeroLagIndicator`
- **Config:** `Length=15`, `AtrLength=16`
- **Input:** `List<PriceHistory>`
- **Output:** `List<ZeroLagResult>` — each has `Date`, `UpSignal`, `DownSignal`
- **Logic:** Computes EMA, then double-smoothes with zero-lag correction. Signal is `UpSignal` when zero-lag EMA > regular EMA.

#### EMA-50 Crossover (`EmaFifty/`)
- **Class:** `EmaFiftyIndicator`
- **Config:** `EmaLength=50`, `VolumeLength=20`
- **Input:** `List<PriceHistory>`
- **Output:** `List<EmaFiftyResult>` — each has `Date`, `UpSignal`
- **Logic:** Detects bullish price crossover above EMA-50 with 20% volume spike confirmation.

#### Support/Resistance Breakout (`BreakOut/`)
- **Class:** `SupportResistanceBreakOutIndicator`
- **Input:** `List<PriceHistory>`
- **Output:** `List<SRResult>` — each has `Date`, `UpSignal`, `DownSignal`
- **Logic:** Uses pivot detection (lookback=20), signed volume analysis, ATR-based box widths. Detects: resistance breakouts (UpSignal), support breakdowns (DownSignal), support holds, resistance holds.
- **Internal model:** `InternalResult` with arrays for Support, Resistance, and boolean signal arrays.

#### RSI-Weighted Moving Average (`Rsima/`)
- **Class:** `RsiWeightedMovingAverageIndicator`
- **Config:** `RsiPeriod=14`, `MaPeriod=55`, `ShowSma=true`
- **Input:** `List<PriceHistory>`
- **Output:** `List<RsiWmaResult>` — each has `Date`, `RsiValue`, `RsiWma`, `Sma`, `DeviationFromSma`, `BullishCross`, `BearishCross`
- **Logic:** Weights closing prices by their RSI/100 value over a 55-bar window. Detects crossovers vs the RSI-WMA line using strict inequalities (Pine Script compatible).

#### Chandelier Exit Oscillator (`ExitOscillator/`)
- **Class:** `ChandelierExitOscillator`
- **Config:** `AtrLength=22`, `Multiplier=3.0`, `Smoothing=3`
- **Input:** `List<PriceHistory>`
- **Output:** `List<ChandelierExitResult>` — each has `Date`, `Direction` (1=Bull/-1=Bear), `ExitLevel`, `Oscillator` (0-100), `DirectionChanged`
- **Logic:** ATR-based trailing stop with direction detection and normalized oscillator.

## Dependencies
- `Ta.Indicator` depends on: `LumenWorks.Framework.IO` (CSV), `Newtonsoft.Json`
- `Ta.CustomIndicator` depends on: `Ta.Indicator`

## Usage Pattern
```csharp
// Core indicator
RSI rsi = new RSI(14);
rsi.PriceHistoryList = priceHistories;
double latestRsi = rsi.Calculate().ResultData.Last().Value.GetValueOrDefault();

// Custom indicator
ZeroLagIndicator zeroLag = new ZeroLagIndicator();
List<ZeroLagResult> results = zeroLag.Calculate(priceHistories);
bool isUpSignal = results.Last().UpSignal;

// Data conversion
List<PriceHistory> weekly = DataConverter.ConvertToWeeklyOHLC(dailyData);
```

## Key Constraints
- Most indicators need a minimum number of bars (usually `Period + 1`)
- RSI output has `Count = input.Count - 1` (starts from bar index 1)
- The ZeroLag indicator accumulates internal state in `Prices` list — instantiate fresh for each stock
- SupportResistance needs at least 20 bars (`lookback` parameter)
- RsiWMA needs at least `max(RsiPeriod+1, MaPeriod+1)` bars (typically 56)
