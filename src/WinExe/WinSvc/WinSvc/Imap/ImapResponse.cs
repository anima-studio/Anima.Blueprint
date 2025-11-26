namespace WinSvc.Imap;

public class ImapResponse
{
    public bool Success { get; set; }
    public string Status { get; set; }
    public string Message { get; set; }
    public bool IsAuthFailure => Message?.Contains("AUTHENTICATIONFAILED") ?? false;
}
