namespace WinSvc.Core;

public interface ILogger
{
    void Info(string message);
    void Error(string message, System.Exception ex = null);
}
