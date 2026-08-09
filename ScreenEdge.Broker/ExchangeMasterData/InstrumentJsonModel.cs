using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace ScreenEdge.Broker;

public class InstrumentJsonModel
{
    private DateTime exp;

    public string token { get; set; }

    public string symbol { get; set; }

    public string name { get; set; }

    public string expiry { get; set; }

    public string strike { get; set; }

    public string lotsize { get; set; }

    public string instrumenttype { get; set; }

    public string exch_seg { get; set; }

    public string tick_size { get; set; }

    public DateTime Expiry_Date
    {
        get
        {
            return !string.IsNullOrEmpty(this.expiry) ? DateTime.ParseExact(this.expiry, "ddMMMyyyy", (IFormatProvider)CultureInfo.InvariantCulture) : DateTime.MinValue;
        }
        set
        {
            this.exp = DateTime.ParseExact(this.expiry, "ddMMMyyyy", (IFormatProvider)CultureInfo.InvariantCulture);
        }
    }

    public static List<InstrumentJsonModel> LoadInstrumets()
    {
        using (StreamReader streamReader = new StreamReader(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/MasterData/OpenAPIScripMaster.json"))
            return JsonSerializer.Deserialize<List<InstrumentJsonModel>>(streamReader.ReadToEnd());
    }

    public static void DownloadOpenAPIScripMaster()
    {
        string? entryAssemblyLocation = Assembly.GetEntryAssembly()?.Location;
        if (entryAssemblyLocation == null)
            throw new InvalidOperationException("Entry assembly location is null.");

        string directory = Path.Combine(Path.GetDirectoryName(entryAssemblyLocation) ?? string.Empty, "MasterData");
        string path = Path.Combine(directory, "OpenAPIScripMaster.json");
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage result1 = httpClient.GetAsync("https://margincalculator.angelbroking.com/OpenAPI_File/files/OpenAPIScripMaster.json").Result;
            result1.EnsureSuccessStatusCode();
            string result2 = result1.Content.ReadAsStringAsync().Result;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, result2);
        }
    }
}
