using System;

namespace WinSvc.Core;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; }
    public string Exception { get; set; }
}
