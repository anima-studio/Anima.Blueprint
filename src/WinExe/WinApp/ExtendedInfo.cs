using System.Collections.Generic;

namespace WinApp;

public class ExtendedInfo
{
    public List<string> WifiAvailable { get; set; }
    public List<string> BluetoothPaired { get; set; }
    public Dictionary<string, double> UsageTimeline { get; set; }
}
