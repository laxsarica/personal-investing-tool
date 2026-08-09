namespace ScreenEdge.Broker;

public class GetMasterData
{
    public static List<InstrumentJsonModel> GetAllInstruments(string exchange, string instrument)
    {
        string[] symbol = instrument.Split('-');
        List<InstrumentJsonModel> source = InstrumentJsonModel.LoadInstrumets();
        List<InstrumentJsonModel> instrumentJsonModelList = new List<InstrumentJsonModel>();
        Func<InstrumentJsonModel, bool> predicate = (Func<InstrumentJsonModel, bool>)(d => d.exch_seg == exchange.ToUpper() && d.symbol.Contains(symbol[0].ToUpper()));
        List<InstrumentJsonModel> list = source.Where<InstrumentJsonModel>(predicate).ToList<InstrumentJsonModel>();
        if (exchange == "NFO")
            list = list.Where<InstrumentJsonModel>((Func<InstrumentJsonModel, bool>)(w => w.name == symbol[1].ToUpper())).OrderBy<InstrumentJsonModel, DateTime>((Func<InstrumentJsonModel, DateTime>)(o => o.Expiry_Date)).ToList<InstrumentJsonModel>();
        return list;
    }

    public static List<InstrumentJsonModel> GetAllNseEquity()
    {
        List<InstrumentJsonModel> source = InstrumentJsonModel.LoadInstrumets();
        List<InstrumentJsonModel> instrumentJsonModelList = new List<InstrumentJsonModel>();
        return source.Where<InstrumentJsonModel>((Func<InstrumentJsonModel, bool>)(d => d.exch_seg == "NSE" && d.instrumenttype == "" && d.symbol.Contains("-EQ") && !d.symbol.Contains("NSETEST"))).OrderBy<InstrumentJsonModel, string>((Func<InstrumentJsonModel, string>)(o => o.symbol)).ToList<InstrumentJsonModel>();
    }
    public static List<InstrumentJsonModel> GetAllIndex()
    {
        List<InstrumentJsonModel> source = InstrumentJsonModel.LoadInstrumets();
        List<InstrumentJsonModel> instrumentJsonModelList = new List<InstrumentJsonModel>();
        return source.Where<InstrumentJsonModel>((Func<InstrumentJsonModel, bool>)(d => d.exch_seg == "NSE" && d.instrumenttype == "AMXIDX")).OrderBy<InstrumentJsonModel, string>((Func<InstrumentJsonModel, string>)(o => o.symbol)).ToList<InstrumentJsonModel>();
    }
    public static List<InstrumentJsonModel> GetAllNfo()
    {
        List<InstrumentJsonModel> source = InstrumentJsonModel.LoadInstrumets();
        List<InstrumentJsonModel> instrumentJsonModelList = new List<InstrumentJsonModel>();
        return source.Where<InstrumentJsonModel>((Func<InstrumentJsonModel, bool>)(d => d.exch_seg == "NFO")).ToList<InstrumentJsonModel>();
    }

    public static List<string> GetNfoStock()
    {
        List<InstrumentJsonModel> source = InstrumentJsonModel.LoadInstrumets();
        List<InstrumentJsonModel> instrumentJsonModelList = new List<InstrumentJsonModel>();
        return source.Where<InstrumentJsonModel>((Func<InstrumentJsonModel, bool>)(d => d.exch_seg == "NFO" && !d.name.Contains("NSETEST") && d.instrumenttype == "OPTSTK")).Select<InstrumentJsonModel, string>((Func<InstrumentJsonModel, string>)(d => d.name)).Distinct<string>().ToList<string>();
    }
}
