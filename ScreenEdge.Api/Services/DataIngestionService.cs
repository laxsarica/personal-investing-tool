using ScreenEdge.Broker;
using ScreenEdge.Broker.Requests;
using ScreenEdge.Broker.Responses;
using ScreenEdge.Entity.Entities;
using ScreenEdge.Repository;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ScreenEdge.Api.Services;

public class DataIngestionService
{
    private readonly IUnitOfWorks _uow;
    private readonly TradeApiCreds _tradeApiCreds;
    private readonly ILogger<DataIngestionService> _logger;

    public DataIngestionService(IUnitOfWorks uow, TradeApiCreds tradeApiCreds, ILogger<DataIngestionService> logger)
    {
        _uow = uow;
        _tradeApiCreds = tradeApiCreds;
        _logger = logger;
    }

    public async Task<string> SyncDailyDataAsync()
    {
        _logger.LogInformation("Starting daily data sync from Angel One...");
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        InstrumentJsonModel.DownloadOpenAPIScripMaster();
        var list = GetMasterData.GetAllNseEquity().OrderBy(o => o.token).ToList();
        
        _logger.LogInformation($"Found {list.Count} NSE Equity instruments. Fetching quotes...");
        int totalInserted = 0;
        int batchSize = 50;
        int numBatches = list.Count / batchSize;

        for (int index = 0; index <= numBatches; ++index)
        {
            await Task.Delay(2000); // Respect API rate limits
            string[] tokens = list.Skip(index * batchSize).Take(batchSize).Select(s => s.token).ToArray();
            
            if (tokens.Length > 0)
            {
                try
                {
                    var response = AngelOneApi.GetLiveQuote(_tradeApiCreds.apiKey, _tradeApiCreds.accessToken, new LiveQuoteRequest()
                    {
                        mode = "FULL",
                        exchangeTokens = { NSE = tokens }
                    });

                    if (response?.data?.fetched != null)
                    {
                        var entities = response.data.fetched.Where(HasVolume).Select(ToTicker).ToList();
                        if (entities.Count > 0)
                        {
                            // Filter out symbols that already have data for today to prevent duplicates
                            var todayDate = entities.First().Date;
                            var symbolsInBatch = entities.Select(e => e.Symbol).Distinct().ToList();
                            var alreadyIngested = _uow.TickerHistoryRepository.Query()
                                .Where(x => x.Date == todayDate && symbolsInBatch.Contains(x.Symbol))
                                .Select(x => x.Symbol)
                                .ToHashSet();

                            entities = entities.Where(e => !alreadyIngested.Contains(e.Symbol)).ToList();
                            if (entities.Count == 0) continue; // All already ingested, skip batch

                            await _uow.TickerHistoryRepository.AddRangeAsync(entities);
                            await _uow.CompleteAsync();
                            totalInserted += entities.Count;
                            
                            // Only update TotalTradingDays for newly inserted symbols
                            var distinctStocks = entities.Select(e => e.Symbol).Distinct();
                            foreach(var sym in distinctStocks)
                            {
                                var existingStock = _uow.DistinctStockRepository.Query().FirstOrDefault(x => x.Symbol == sym);
                                if (existingStock == null)
                                {
                                    var instr = list.FirstOrDefault(x => x.symbol == sym + "-EQ");
                                    await _uow.DistinctStockRepository.AddAsync(new DistinctStock 
                                    { 
                                        Symbol = sym, 
                                        CompanyName = instr?.name ?? sym, 
                                        Exchange = "NSE",
                                        TotalTradingDays = 1 // First day
                                    });
                                }
                                else
                                {
                                    existingStock.TotalTradingDays += 1;
                                    await _uow.DistinctStockRepository.UpdateAsync(existingStock);
                                }
                            }
                            await _uow.CompleteAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching quotes for batch {index}");
                }
            }
        }

        stopwatch.Stop();
        string result = $"Successfully ingested {totalInserted} ticker records in {stopwatch.Elapsed.TotalMinutes:F2} minutes.";
        _logger.LogInformation(result);
        return result;
    }

    public async Task<string> SyncHistoricalDataAsync(string symbol, DateTime fromDate)
    {
        var toDate = DateTime.Today;
        _logger.LogInformation($"Starting historical data sync for {symbol} from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");
        Stopwatch stopwatch = Stopwatch.StartNew();

        InstrumentJsonModel.DownloadOpenAPIScripMaster();
        var instrument = GetMasterData.GetAllNseEquity().FirstOrDefault(x => x.symbol == symbol + "-EQ" || x.symbol == symbol);
        
        if (instrument == null)
        {
            return $"Error: Could not find token for symbol {symbol}";
        }

        var request = new HistoryDataRequest
        {
            exchange = "NSE",
            symboltoken = instrument.token,
            interval = "ONE_DAY",
            fromdate = fromDate.ToString("yyyy-MM-dd HH:mm"),
            todate = toDate.ToString("yyyy-MM-dd HH:mm")
        };

        try
        {
            var response = AngelOneApi.GetHistoricalData(_tradeApiCreds.apiKey, _tradeApiCreds.accessToken, request);

            if (response != null && response.status && response.data != null)
            {
                var entities = new List<TickerHistory>();
                foreach (var row in response.data)
                {
                    // row format: [ timestamp, open, high, low, close, volume ]
                    if (row.Length >= 6 && DateTime.TryParse(row[0]?.ToString(), out DateTime date))
                    {
                        entities.Add(new TickerHistory
                        {
                            Date = date.Date,
                            Symbol = symbol,
                            Open = decimal.Parse(row[1]?.ToString() ?? "0"),
                            High = decimal.Parse(row[2]?.ToString() ?? "0"),
                            Low = decimal.Parse(row[3]?.ToString() ?? "0"),
                            Close = decimal.Parse(row[4]?.ToString() ?? "0"),
                            Volume = decimal.Parse(row[5]?.ToString() ?? "0")
                        });
                    }
                }

                if (entities.Count > 0)
                {
                    // Delete existing records for this date range to prevent duplicates
                    var existing = _uow.TickerHistoryRepository.Query()
                        .Where(x => x.Symbol == symbol && x.Date >= fromDate.Date && x.Date <= toDate.Date)
                        .ToList();
                    
                    if (existing.Count > 0)
                    {
                        _uow.TickerHistoryRepository.RemoveRange(existing);
                    }

                    await _uow.TickerHistoryRepository.AddRangeAsync(entities);
                    
                    // Ensure it's in DistinctStocks and update TotalTradingDays
                    var existingStock = _uow.DistinctStockRepository.Query().FirstOrDefault(x => x.Symbol == symbol);
                    if (existingStock == null)
                    {
                        await _uow.DistinctStockRepository.AddAsync(new DistinctStock 
                        { 
                            Symbol = symbol, 
                            CompanyName = instrument.name ?? symbol, 
                            Exchange = "NSE",
                            TotalTradingDays = entities.Count
                        });
                    }
                    else
                    {
                        existingStock.TotalTradingDays = entities.Count;
                        await _uow.DistinctStockRepository.UpdateAsync(existingStock);
                    }
                    
                    await _uow.CompleteAsync();
                }

                stopwatch.Stop();
                string result = $"Successfully ingested {entities.Count} historical records for {symbol} in {stopwatch.Elapsed.TotalSeconds:F2} seconds.";
                _logger.LogInformation(result);
                return result;
            }
            
            return $"Error or no data returned: {response?.message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching historical data for {symbol}");
            return $"Exception occurred: {ex.Message}";
        }
    }

    public async Task<string> SyncAllHistoricalDataAsync(DateTime fromDate, int? limit = null)
    {
        var toDate = DateTime.Today;
        _logger.LogInformation($"Starting bulk historical data sync for all NSE symbols from {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}...");
        Stopwatch stopwatch = Stopwatch.StartNew();

        InstrumentJsonModel.DownloadOpenAPIScripMaster();
        var query = GetMasterData.GetAllNseEquity().OrderBy(o => o.token).AsEnumerable();
        
        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }
        
        var allSymbols = query.ToList();
        
        _logger.LogInformation($"Found {allSymbols.Count} NSE Equity instruments to sync.");
        
        int successCount = 0;
        int failureCount = 0;
        int requestCount = 0;

        foreach (var instrument in allSymbols)
        {
            try
            {
                // Clean the symbol name by removing "-EQ" if present for consistency
                var symbol = instrument.symbol.Replace("-EQ", "");
                var result = await SyncHistoricalDataAsync(symbol, fromDate);
                
                if (result.StartsWith("Successfully"))
                    successCount++;
                else
                    failureCount++;
                
                requestCount++;
                // Rate Limiting: Burst 3 requests, then wait 1 second (Angel One limits to 3 requests/sec)
                if (requestCount % 3 == 0)
                {
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process historical data for {instrument.symbol}");
                failureCount++;
            }
        }

        stopwatch.Stop();
        string summary = $"Bulk historical sync completed in {stopwatch.Elapsed.TotalMinutes:F2} minutes. " +
                         $"Successful symbols: {successCount}, Failed symbols: {failureCount}.";
        _logger.LogInformation(summary);
        return summary;
    }

    /// <summary>
    /// Returns the most recent date in TickerHistories. Used by the daily job
    /// to detect whether fresh market data was received (holiday guard).
    /// </summary>
    public DateTime? GetLatestTickerDate()
    {
        var latest = _uow.TickerHistoryRepository.Query()
            .OrderByDescending(x => x.Date)
            .Select(x => x.Date)
            .FirstOrDefault();

        return latest == default ? null : latest;
    }

    private static bool HasVolume(Fetched f) => long.TryParse(f.tradeVolume.ToString(), out long vol) && vol > 0;

    private static TickerHistory ToTicker(Fetched f)
    {
        return new TickerHistory
        {
            Date = f.exchFeedTime.Date,
            Symbol = f.tradingSymbol.Replace("-EQ", ""), // Normalize symbol
            Open = decimal.Parse(f.open.ToString()),
            High = decimal.Parse(f.high.ToString()),
            Low = decimal.Parse(f.low.ToString()),
            Close = decimal.Parse(f.ltp.ToString()),
            Volume = decimal.Parse(f.tradeVolume.ToString())
        };
    }
}
