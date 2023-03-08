namespace LibLauncherUtil;

public class OnLoggedInEventArgs : EventArgs
{
    public List<string>? Characters { get; set; }
    public string? Username { get; set; }
    public bool AutoLogin { get; set; }

    public OnLoggedInEventArgs(List<string> characters, string username, bool autoLogin)
    {
        Characters = characters;
        Username = username;
        AutoLogin = autoLogin;
    }
}

public class OnLoginFailedEventArgs : EventArgs
{
    public string Reason { get; set; }

    public OnLoginFailedEventArgs(string reason)
    {
        Reason = reason;
    }
}

public class OnAccountCreatedEventArgs : EventArgs
{
    public string Status { get; set; }

    public OnAccountCreatedEventArgs(string status)
    {
        Status = status;
    }
}

public class OnAccountCreateFailedEventArgs : EventArgs
{
    public string Reason { get; set; }

    public OnAccountCreateFailedEventArgs(string reason)
    {
        Reason = reason;
    }
}

public class OnLoggedInMessageEventArgs : EventArgs
{
    public string Message { get; set; }

    public OnLoggedInMessageEventArgs(string message)
    {
        Message = message;
    }
}
