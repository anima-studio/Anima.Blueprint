using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinSvc.Core;

public class StealthLogger : ILogger
{
    private readonly StateManager _stateManager;
    private readonly ConcurrentQueue<LogEntry> _pendingLogs = new ConcurrentQueue<LogEntry>();
    private const int MAX_PENDING = 1000;

    public StealthLogger(StateManager stateManager)
    {
        _stateManager = stateManager;
    }

    public void Info(string message)
    {
        EnqueueLog(LogLevel.Info, message, null);
    }

    public void Error(string message, Exception ex = null)
    {
        EnqueueLog(LogLevel.Error, message, ex);
    }

    private void EnqueueLog(LogLevel level, string message, Exception ex)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Exception = ex?.ToString()
        };

        _pendingLogs.Enqueue(entry);

        // Limit memory - drop oldest if too many
        while (_pendingLogs.Count > MAX_PENDING)
        {
            _pendingLogs.TryDequeue(out _);
        }

        // Persist critical errors immediately
        if (level == LogLevel.Error)
        {
            _stateManager.AppendLog(entry);
        }

#if DEBUG
        Console.WriteLine($"[{level} {DateTime.Now:HH:mm:ss}] {message}");
        if (ex != null) Console.WriteLine(ex);
#endif
    }

    public string FlushToString()
    {
        var sb = new StringBuilder();
        var logs = _pendingLogs.ToArray();

        foreach (var log in logs.OrderBy(l => l.Timestamp))
        {
            sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Level}: {log.Message}");
            if (!string.IsNullOrEmpty(log.Exception))
            {
                sb.AppendLine($"  Exception: {log.Exception}");
            }
        }

        return sb.ToString();
    }

    public void ClearPending()
    {
        while (_pendingLogs.TryDequeue(out _)) { }
    }
}
