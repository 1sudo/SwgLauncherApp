﻿using System.Diagnostics;

namespace LibLauncherApp.Util;

public enum LogType
{
    DEBUG = 0,
    CRITICAL = 1,
    ERROR = 2,
    WARNING = 3,
    INFO = 4
}

public class Logger
{
    public static event EventHandler<OnLoggedInMessageEventArgs>? OnLoggedInMessage;

    private static Logger? instance = null;

    public static Logger Instance
    {
        get
        {
            return instance ??= new Logger();
        }
    }

    public Logger()
    {
        string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logFilePath = Path.Combine(appDataFolder, "LauncherApp", "trace.log");
        Trace.Listeners.Add(new TextWriterTraceListener(logFilePath));
    }

    public void Log<T>(T message, LogType logType = INFO)
    {
        string date = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt zzz");

        string header = logType switch
        {
            DEBUG => "[DEBUG] - " + date,
            CRITICAL => "[Critical] - " + date,
            ERROR => "[ERROR] - " + date,
            WARNING => "[WARNING] - " + date,
            INFO => "[INFO] - " + date,
            _ => "[INFO] - " + date,
        };

        Trace.WriteLine(header);

        string msg = string.Empty;

        if (message is string)
        {
            msg = message as string ?? "";
        }

        if (message is Exception)
        {
            var e = (message as Exception);
                
            if (e is not null)
            {
                if (e.StackTrace is not null)
                {
                    msg = e.StackTrace;
                }
                else
                {
                    msg = e.Message;
                }
            }
        }

        Trace.WriteLine(message);

        Trace.Flush();

        OnLoggedInMessage?.Invoke(this, new OnLoggedInMessageEventArgs($"{header}\n {message}\n\n"));
    }
}
