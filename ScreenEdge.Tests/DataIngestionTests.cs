using EFCore.BulkExtensions;
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
    private const string ConnectionString = "Server=localhost;Database=ScreenEdgeDb;Trusted_Connection=True;TrustServerCertificate=True;";

    public DataIngestionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
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
        historyDataRequest.fromdate = DateTime.Now.AddYears(-5).ToString("yyyy-MM-dd HH:mm");
        historyDataRequest.todate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

        foreach (InstrumentJsonModel item in list)
        {
            count++;
            try
            {
                historyDataRequest.symboltoken = item.token;
                _output.WriteLine($"Inserting {item.symbol} - {item.token}");
                // Assuming AngelOneApi is the static class in ScreenEdge.Broker
                var result = AngelOneApi.GetHistoricalData(tradeApiCreds.apiKey, tradeApiCreds.accessToken, historyDataRequest);
                if (result.status)
                {
                    string cleanSymbol = item.symbol.Replace("-EQ", "");
                    this.UploadDataLive(cleanSymbol, item.name, result);
                }
                if (count % 3 == 0)
                {
                    Thread.Sleep(3000);
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
        using (var context = CreateDbContext())
        {
            List<TickerHistory> entities = new List<TickerHistory>();
            foreach (var fetched in historyDataResponse.data)
            {
                string s = fetched[0].ToString();
                entities.Add(new TickerHistory()
                {
                    Date = DateTime.Parse(s),
                    Symbol = symbol,
                    Open = Decimal.Parse(fetched[1].ToString()),
                    High = Decimal.Parse(fetched[2].ToString()),
                    Low = Decimal.Parse(fetched[3].ToString()),
                    Close = Decimal.Parse(fetched[4].ToString()),
                    Volume = Decimal.Parse(fetched[5].ToString())
                });
            }
            var orderedData = entities.OrderBy(o => o.Date).ToList();
            context.BulkInsert(orderedData);

            // Add DistinctStock code
            var existingStock = context.DistinctStocks.FirstOrDefault(x => x.Symbol == symbol);
            if (existingStock == null)
            {
                context.DistinctStocks.Add(new DistinctStock 
                { 
                    Symbol = symbol, 
                    CompanyName = companyName ?? symbol, 
                    Exchange = "NSE",
                    TotalTradingDays = entities.Count
                });
            }
            else
            {
                existingStock.TotalTradingDays = entities.Count;
                context.DistinctStocks.Update(existingStock);
            }
            context.SaveChanges();
        }
    }
}
