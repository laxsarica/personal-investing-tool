using Ta.Indicator.Base;

namespace Ta.Indicator.BaseFunction;

public abstract class IndicatorCalculatorBase<T>
{
    public abstract List<PriceHistory> PriceHistoryList { get; set; }

    public virtual void Load(string path, string dataType = "File")
    {
        if (dataType == "File")
        {
            string extension = Path.GetExtension(path);
            if (extension == ".csv")
                this.PriceHistoryList = DataConverter.ConvertFromCsv(path);
            if (!(extension == ".json"))
                return;
            this.PriceHistoryList = DataConverter.ConvertFromJson(path);
        }
        else
            this.PriceHistoryList = DataConverter.ConvertFromString(path);
    }

    public abstract T Calculate();
}
