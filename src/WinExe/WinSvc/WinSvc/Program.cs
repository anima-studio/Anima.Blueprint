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
        var service = new Service1();
        service.DebugStart(args);
        Console.WriteLine("Press ENTER to stop...");
        Console.ReadLine();
        service.DebugStop();
#else
        ServiceBase.Run(new Service1());
#endif
    }
}
