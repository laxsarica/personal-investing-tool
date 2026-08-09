namespace Ta.Indicator.Base;

public class Rootobject
{
    public bool status { get; set; }
    public string message { get; set; } = string.Empty;
    public string errorcode { get; set; } = string.Empty;
    public object[][] data { get; set; } = Array.Empty<object[]>();
}
