using System;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace WinSvc.Services;

public class SystemInfoCollector
{
    public string GetLabel()
    {
        return $"{Environment.MachineName}_{GetCurrentUser()}";
    }

    public string CollectReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Machine: {Environment.MachineName}");
        sb.AppendLine($"User: {GetCurrentUser()}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"IP: {GetPrimaryIp()}");
        sb.AppendLine($"Uptime: {GetUptime():F1}h");
        sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        return sb.ToString();
    }

    private string GetCurrentUser()
    {
        try
        {
            using (var wmi = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem"))
            {
                foreach (ManagementObject obj in wmi.Get())
                {
                    return obj["UserName"]?.ToString()?.Split('\\').Last() ?? Environment.UserName;
                }
            }
        }
        catch { }
        return Environment.UserName;
    }

    private string GetPrimaryIp()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address.ToString())
                .FirstOrDefault() ?? "0.0.0.0";
        }
        catch { return "0.0.0.0"; }
    }

    private double GetUptime()
    {
        try
        {
            using (var wmi = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in wmi.Get())
                {
                    var boot = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    return (DateTime.Now - boot).TotalHours;
                }
            }
        }
        catch { }
        return 0;
    }
}
