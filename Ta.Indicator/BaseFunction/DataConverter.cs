using LumenWorks.Framework.IO.Csv;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ta.Indicator.Base;

namespace Ta.Indicator.BaseFunction;

public class DataConverter
{
    public static List<PriceHistory> ConvertFromCsv(string path)
    {
        using (CsvReader csvReader = new CsvReader((TextReader)new StreamReader(path), true))
        {
            int fieldCount = csvReader.FieldCount;
            string[] fieldHeaders = csvReader.GetFieldHeaders();
            List<PriceHistory> priceHistoryList = new List<PriceHistory>();
            while (csvReader.ReadNextRecord())
            {
                PriceHistory priceHistory = new PriceHistory();
                for (int field = 0; field < fieldCount; ++field)
                {
                    switch (fieldHeaders[field])
                    {
                        case "Date":
                            priceHistory.Date = new DateTime(int.Parse(csvReader[field].Substring(0, 4)), int.Parse(csvReader[field].Substring(5, 2)), int.Parse(csvReader[field].Substring(8, 2)));
                            break;
                        case "Open":
                            priceHistory.Open = double.Parse(csvReader[field], (IFormatProvider)CultureInfo.InvariantCulture);
                            break;
                        case "High":
                            priceHistory.High = double.Parse(csvReader[field], (IFormatProvider)CultureInfo.InvariantCulture);
                            break;
                        case "Low":
                            priceHistory.Low = double.Parse(csvReader[field], (IFormatProvider)CultureInfo.InvariantCulture);
                            break;
                        case "Close":
                            priceHistory.Close = double.Parse(csvReader[field], (IFormatProvider)CultureInfo.InvariantCulture);
                            break;
                        case "Volume":
                            priceHistory.Volume = (double)int.Parse(csvReader[field]);
                            break;
                    }
                }
                priceHistoryList.Add(priceHistory);
            }
            return priceHistoryList;
        }
    }

    public static List<PriceHistory> ConvertFromJson(string path)
    {
        Rootobject rootobject = JsonConvert.DeserializeObject<Rootobject>(File.ReadAllText(path));
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        foreach (object[] objArray in rootobject.data)
            priceHistoryList.Add(new PriceHistory()
            {
                Date = DateTime.Parse(objArray[0].ToString()),
                Open = Convert.ToDouble(objArray[1]),
                High = Convert.ToDouble(objArray[2]),
                Low = Convert.ToDouble(objArray[3]),
                Close = Convert.ToDouble(objArray[4]),
                Volume = (double)Convert.ToInt64(objArray[5])
            });
        return priceHistoryList;
    }

    public static List<PriceHistory> ConvertFromString(string jsonContent)
    {
        Rootobject rootobject = JsonConvert.DeserializeObject<Rootobject>(jsonContent);
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        foreach (object[] objArray in rootobject.data)
            priceHistoryList.Add(new PriceHistory()
            {
                Date = DateTime.Parse(objArray[0].ToString()),
                Open = Convert.ToDouble(objArray[1]),
                High = Convert.ToDouble(objArray[2]),
                Low = Convert.ToDouble(objArray[3]),
                Close = Convert.ToDouble(objArray[4]),
                Volume = (double)Convert.ToInt64(objArray[5])
            });
        return priceHistoryList;
    }

    public static List<PriceHistory> ConvertToWeeklyOHLC1(List<PriceHistory> dailyData)
    {
        List<PriceHistory> weeklyOhlC1 = new List<PriceHistory>();
        DateTime minValue = DateTime.MinValue;
        PriceHistory priceHistory1 = (PriceHistory)null;
        int num = 0;
        foreach (PriceHistory priceHistory2 in dailyData)
        {
            int iso8601WeekOfYear = DataConverter.GetIso8601WeekOfYear(priceHistory2.Date);
            if (num != iso8601WeekOfYear)
            {
                priceHistory1 = new PriceHistory()
                {
                    Date = priceHistory2.Date,
                    Open = priceHistory2.Open,
                    High = priceHistory2.High,
                    Low = priceHistory2.Low,
                    Close = priceHistory2.Close,
                    Volume = priceHistory2.Volume
                };
                weeklyOhlC1.Add(priceHistory1);
                num = iso8601WeekOfYear;
            }
            else if (num == iso8601WeekOfYear)
            {
                priceHistory1.High = Math.Max(priceHistory1.High, priceHistory2.High);
                priceHistory1.Low = Math.Min(priceHistory1.Low, priceHistory2.Low);
                priceHistory1.Close = priceHistory2.Close;
                priceHistory1.Volume += priceHistory2.Volume;
            }
        }
        return weeklyOhlC1;
    }

    public static List<PriceHistory> ConvertToWeeklyOHLC(List<PriceHistory> dailyData)
    {
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        return dailyData.GroupBy(x => new
        {
            Year = x.Date.Year,
            Week = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(x.Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
        }).Select(g => new PriceHistory()
        {
            Date = g.Max(x => x.Date),
            Open = g.First().Open,
            High = g.Max(x => x.High),
            Low = g.Min(x => x.Low),
            Close = g.Last().Close,
            Volume = g.Sum(x => x.Volume)
        }).ToList();
    }

    public static List<PriceHistory> ConvertToMonthlyOHLC(List<PriceHistory> dailyData)
    {
        List<PriceHistory> priceHistoryList = new List<PriceHistory>();
        return dailyData.GroupBy(x =>
        {
            DateTime date = x.Date;
            int year = date.Year;
            date = x.Date;
            int month = date.Month;
            return new { Year = year, Month = month };
        }).Select(g => new PriceHistory()
        {
            Date = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)),
            Open = g.First().Open,
            High = g.Max(x => x.High),
            Low = g.Min(x => x.Low),
            Close = g.Last().Close,
            Volume = g.Sum(x => x.Volume)
        }).ToList();
    }

    private static int GetIso8601WeekOfYear(DateTime date)
    {
        return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
}
