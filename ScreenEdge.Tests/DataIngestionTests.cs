using ScreenEdge.Broker;
using ScreenEdge.Broker.Requests;
using ScreenEdge.Broker.Responses;
using ScreenEdge.Entity;
using ScreenEdge.Entity.Entities;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace ScreenEdge.Tests;

public class DataIngestionTests
{
    private readonly ITestOutputHelper _output;
    private const string ConnectionString = "Host=db.getvoroa.com;Port=25524;Database=pg_screener_prod;Username=postgres;Password=gKzmiSPjxLeswnZtbYvVthRag37zvZ52;SSL Mode=Require;Trust Server Certificate=true;";

    static DataIngestionTests()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public DataIngestionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }



    [Fact]
    public void CheckDatabaseCounts()
    {
        using var context = CreateDbContext();
        var conn = context.Database.GetDbConnection();
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT pg_is_in_recovery(), current_user, current_database(), inet_server_addr(), inet_server_port();";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                _output.WriteLine($"[PG STATUS] IsInRecovery: {reader[0]}, User: {reader[1]}, DB: {reader[2]}, Server: {reader[3]}:{reader[4]}");
            }
        }
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SHOW default_transaction_read_only; SHOW transaction_read_only;";
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                _output.WriteLine($"[PG READONLY] default_transaction_read_only: {reader[0]}");
            if (reader.NextResult() && reader.Read())
                _output.WriteLine($"[PG READONLY] transaction_read_only: {reader[0]}");
        }

        var distinctStocks = context.DistinctStocks.Count();
        var tickerHistories = context.TickerHistories.Count();
        var screeners = context.Screeners.Count();
        var fundamentals = context.StockFundamentals.Count();
        
        var sampleStock = context.DistinctStocks.FirstOrDefault();
        var sampleCandles = context.TickerHistories.Take(3).ToList();

        _output.WriteLine("==================================================");
        _output.WriteLine($"[DB RECORD COUNTS]");
        _output.WriteLine($" - DistinctStocks:  {distinctStocks}");
        _output.WriteLine($" - TickerHistories: {tickerHistories}");
        _output.WriteLine($" - Screeners:       {screeners}");
        _output.WriteLine($" - Fundamentals:    {fundamentals}");
        if (sampleStock != null)
        {
            _output.WriteLine($"Sample Stock: {sampleStock.Symbol} ({sampleStock.CompanyName}), Trading Days: {sampleStock.TotalTradingDays}");
        }
        foreach (var c in sampleCandles)
        {
            _output.WriteLine($"Sample Candle: {c.Symbol} | Date: {c.Date:yyyy-MM-dd} | Close: {c.Close} | Vol: {c.Volume}");
        }
        _output.WriteLine("==================================================");
    }



    [Fact]
    public void CleanUpTestSymbol()
    {
        using var context = CreateDbContext();
        var testSym = context.DistinctStocks.FirstOrDefault(x => x.Symbol == "TEST_SYM");
        if (testSym != null)
        {
            context.DistinctStocks.Remove(testSym);
            context.SaveChanges();
        }
    }





    [Fact]
    public void TestSingleStockIngestion()
    {
        InstrumentJsonModel.DownloadOpenAPIScripMaster();
        List<InstrumentJsonModel> list = GetMasterData.GetAllNseEquity().OrderBy(o => o.token).ToList();
        var brokerSettings = new BrokerSettings
        {
            ApiKey = "nhW8pN8W",
            Password = "1919",
            TotpSecret = "RSOTHTSD2BHGYF7VYPLSYDS5JY",
            KiteApiKey = "e0kdq3b20oii47ni",
            KiteApiSecret = "ls36iqszj3325uofv8xjg1cpb4mv7fj3"
        };
        TradeApiCreds tradeApiCreds = new TradeApiCreds(Microsoft.Extensions.Options.Options.Create(brokerSettings));
        _output.WriteLine($"Token acquired: {!string.IsNullOrEmpty(tradeApiCreds.accessToken)}");

        var testItem = list.FirstOrDefault(x => x.symbol == "RELIANCE-EQ") ?? list.First();
        var historyDataRequest = new HistoryDataRequest
        {
            exchange = "NSE",
            interval = "ONE_DAY",
            symboltoken = testItem.token,
            fromdate = DateTime.Now.AddYears(-3).ToString("yyyy-MM-dd HH:mm"),
            todate = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
        };

        var result = AngelOneApi.GetHistoricalData(tradeApiCreds.apiKey, tradeApiCreds.accessToken, historyDataRequest);
        _output.WriteLine($"Angel One API result status: {result?.status}, Message: {result?.message}");
        if (result != null && result.status && result.data != null)
        {
            string cleanSymbol = testItem.symbol.Replace("-EQ", "");
            UploadDataLive(cleanSymbol, testItem.name, result);
            _output.WriteLine($"Successfully uploaded {result.data.Length} candles for {cleanSymbol}");
        }
    }

    [Fact]
    public void InsertHistoricalData()
    {
        InstrumentJsonModel.DownloadOpenAPIScripMaster();
        List<InstrumentJsonModel> list = GetMasterData.GetAllNseEquity().OrderBy(o => o.token).ToList();
        Stopwatch stopwatch = Stopwatch.StartNew();
        var brokerSettings = new BrokerSettings
        {
            ApiKey = "nhW8pN8W",
            Password = "1919",
            TotpSecret = "RSOTHTSD2BHGYF7VYPLSYDS5JY",
            KiteApiKey = "e0kdq3b20oii47ni",
            KiteApiSecret = "ls36iqszj3325uofv8xjg1cpb4mv7fj3"
        };
        TradeApiCreds tradeApiCreds = new TradeApiCreds(Microsoft.Extensions.Options.Options.Create(brokerSettings));
        int count = 0;
        HistoryDataRequest historyDataRequest = new HistoryDataRequest();
        historyDataRequest.exchange = "NSE";
        historyDataRequest.interval = "ONE_DAY";
        historyDataRequest.fromdate = DateTime.Now.AddYears(-3).ToString("yyyy-MM-dd HH:mm");
        historyDataRequest.todate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        using var checkContext = CreateDbContext();
        var completedSymbols = checkContext.DistinctStocks
            .Where(x => x.TotalTradingDays >= 200)
            .Select(x => x.Symbol)
            .ToHashSet();

        _output.WriteLine($"Found {list.Count} NSE Equity instruments. {completedSymbols.Count} already ingested.");

        foreach (InstrumentJsonModel item in list)
        {
            count++;
            string cleanSymbol = item.symbol.Replace("-EQ", "");
            if (completedSymbols.Contains(cleanSymbol))
            {
                continue;
            }

            try
            {
                historyDataRequest.symboltoken = item.token;
                var logMsg = $"[{count}/{list.Count}] Ingesting {cleanSymbol} (token: {item.token})...";
                _output.WriteLine(logMsg);
                Console.WriteLine(logMsg);

                var result = AngelOneApi.GetHistoricalData(tradeApiCreds.apiKey, tradeApiCreds.accessToken, historyDataRequest);
                if (result != null && result.status && result.data != null)
                {
                    this.UploadDataLive(cleanSymbol, item.name, result);
                    var savedMsg = $" -> [{cleanSymbol}] Saved {result.data.Length} candles to PostgreSQL";
                    _output.WriteLine(savedMsg);
                    Console.WriteLine(savedMsg);
                }
                else
                {
                    var errMsg = $" -> [{cleanSymbol}] No data or error: {result?.message}";
                    _output.WriteLine(errMsg);
                    Console.WriteLine(errMsg);
                }

                if (count % 3 == 0)
                {
                    Thread.Sleep(1000); // 3 requests/sec Angel One rate limit
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error inserting {item.symbol} - {item.token} : {ex.Message}");
            }
        }
    }

    private void UploadDataLive(string symbol, string companyName, HistoryDataResponse historyDataResponse)
    {
        if (historyDataResponse.data == null || historyDataResponse.data.Length == 0) return;

        using (var context = CreateDbContext())
        {
            var existingDates = context.TickerHistories
                .Where(x => x.Symbol == symbol)
                .Select(x => x.Date)
                .ToHashSet();

            List<TickerHistory> entities = new List<TickerHistory>();
            foreach (var fetched in historyDataResponse.data)
            {
                if (fetched == null || fetched.Length < 6) continue;
                string s = fetched[0].ToString() ?? "";
                if (string.IsNullOrEmpty(s)) continue;

                var date = DateTime.Parse(s);
                if (existingDates.Contains(date)) continue;

                entities.Add(new TickerHistory()
                {
                    Date = date,
                    Symbol = symbol,
                    Open = Decimal.Parse(fetched[1].ToString() ?? "0"),
                    High = Decimal.Parse(fetched[2].ToString() ?? "0"),
                    Low = Decimal.Parse(fetched[3].ToString() ?? "0"),
                    Close = Decimal.Parse(fetched[4].ToString() ?? "0"),
                    Volume = Decimal.Parse(fetched[5].ToString() ?? "0")
                });
            }

            var orderedData = entities
                .GroupBy(o => o.Date)
                .Select(g => g.First())
                .OrderBy(o => o.Date)
                .ToList();

            if (orderedData.Count > 0)
            {
                context.TickerHistories.AddRange(orderedData);
            }

            int totalDays = existingDates.Count + orderedData.Count;

            // Add or update DistinctStock
            var existingStock = context.DistinctStocks.FirstOrDefault(x => x.Symbol == symbol);
            if (existingStock == null)
            {
                context.DistinctStocks.Add(new DistinctStock 
                { 
                    Symbol = symbol, 
                    CompanyName = companyName ?? symbol, 
                    Exchange = "NSE",
                    TotalTradingDays = totalDays
                });
            }
            else
            {
                existingStock.TotalTradingDays = totalDays;
                context.DistinctStocks.Update(existingStock);
            }
            context.SaveChanges();
        }
    }
}
