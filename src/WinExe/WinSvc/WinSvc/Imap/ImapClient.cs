using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

using WinSvc.Configuration;
using WinSvc.Core;

namespace WinSvc.Imap;

public class ImapClient : IDisposable
{
    private readonly GmailConfig _config;
    private readonly ILogger _logger;
    private TcpClient _client;
    private SslStream _stream;
    private int _tag = 1;

    public bool IsConnected => _client?.Connected ?? false;

    public ImapClient(GmailConfig config, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public ImapResponse Connect()
    {
        try
        {
            _client = new TcpClient(_config.ImapHost, _config.ImapPort);
            _stream = new SslStream(_client.GetStream(), false, (s, c, ch, e) => true);
            _stream.AuthenticateAsClient(_config.ImapHost);
            ReadResponse();
            return new ImapResponse { Success = true };
        }
        catch (Exception ex)
        {
            _logger.Error("IMAP connect failed", ex);
            return new ImapResponse { Success = false, Message = ex.Message };
        }
    }

    public ImapResponse Login()
    {
        return SendCommand($"LOGIN \"{_config.Username}\" \"{_config.Password}\"");
    }

    public ImapResponse AppendDraft(string content)
    {
        var resp = SendCommand($"APPEND \"[Gmail]/Drafts\" (\\Draft) {{{content.Length}}}");
        if (resp.Success || resp.Message?.Contains("Ready") == true)
        {
            return SendRaw(content);
        }
        return resp;
    }

    private ImapResponse SendCommand(string cmd)
    {
        try
        {
            var tag = $"A{_tag++:D3}";
            var full = $"{tag} {cmd}\r\n";
            var bytes = Encoding.ASCII.GetBytes(full);
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Flush();

            var raw = ReadResponse();
            return ParseResponse(tag, raw);
        }
        catch (Exception ex)
        {
            _logger.Error($"Command failed: {cmd.Substring(0, Math.Min(20, cmd.Length))}", ex);
            return new ImapResponse { Success = false, Message = ex.Message };
        }
    }

    private ImapResponse SendRaw(string data)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(data + "\r\n");
            _stream.Write(bytes, 0, bytes.Length);
            _stream.Flush();
            var raw = ReadResponse();
            return ParseResponse("RAW", raw);
        }
        catch (Exception ex)
        {
            _logger.Error("Raw send failed", ex);
            return new ImapResponse { Success = false, Message = ex.Message };
        }
    }

    private string ReadResponse()
    {
        var buf = new byte[8192];
        var read = _stream.Read(buf, 0, buf.Length);
        return Encoding.ASCII.GetString(buf, 0, read);
    }

    private ImapResponse ParseResponse(string expectedTag, string raw)
    {
        var lines = raw.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var last = lines.LastOrDefault() ?? "";

        var match = Regex.Match(last, @"^(\S+)\s+(OK|NO|BAD)\s*(.*)$");
        if (match.Success)
        {
            return new ImapResponse
            {
                Success = match.Groups[2].Value == "OK",
                Status = match.Groups[2].Value,
                Message = match.Groups[3].Value.Trim()
            };
        }

        if (raw.TrimStart().StartsWith("+"))
        {
            return new ImapResponse { Success = true, Message = "Ready" };
        }

        return new ImapResponse { Success = false, Status = "UNKNOWN", Message = raw };
    }

    public void Dispose()
    {
        try { SendCommand("LOGOUT"); } catch { }
        _stream?.Dispose();
        _client?.Dispose();
    }
}
