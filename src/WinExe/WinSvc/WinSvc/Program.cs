using System;

namespace WinSvc;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main(string[] args)
    {
        //ServiceBase[] ServicesToRun;
        //ServicesToRun = new ServiceBase[]
        //{
        //    new Service1()
        //};
        //ServiceBase.Run(ServicesToRun);

#if DEBUG
        var svc = new Service1();
        svc.DebugStart(args);
        Console.WriteLine("Running... Press ENTER to stop.");
        Console.ReadLine();
        svc.DebugStop();
#else
        ServiceBase.Run(new RemoteAccessService());
#endif
    }
}
