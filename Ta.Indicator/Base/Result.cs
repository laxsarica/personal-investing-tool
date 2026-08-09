using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ta.Indicator.Base;

public class Result
{
    public List<TimeSeriesData> ResultData { get; set; }
    public Result() => this.ResultData = new List<TimeSeriesData>();
}
