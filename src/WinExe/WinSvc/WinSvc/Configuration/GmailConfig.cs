using System;
using System.Configuration;

namespace WinSvc.Configuration;

public class GmailConfig
{
    public string Username { get; }
    public string Password { get; }
    public string ImapHost { get; }
    public int ImapPort { get; }

    public GmailConfig()
    {
        Username = GetRequired("Gmail:Username");
        Password = GetRequired("Gmail:Password");
        ImapHost = GetOrDefault("Gmail:ImapHost", "imap.gmail.com");
        ImapPort = int.Parse(GetOrDefault("Gmail:ImapPort", "993"));
    }

    private string GetRequired(string key)
    {
        var value = ConfigurationManager.AppSettings[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{key} not configured");
        return value;
    }

    private string GetOrDefault(string key, string defaultValue)
    {
        return ConfigurationManager.AppSettings[key] ?? defaultValue;
    }
}
