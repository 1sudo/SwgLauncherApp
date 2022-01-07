namespace LauncherManagement
{
    public enum LogType
    {
        CRITICAL = 0,
        ERROR = 1,
        WARNING = 2,
        INFO = 3
    }

    public static class LogHandler
    {
        public static async Task Log(LogType logType, string logText)
        {
            string header = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt zzz");

            switch (logType)
            {
                case LogType.CRITICAL: header += " [Critical] "; break;
                case LogType.ERROR: header += " [Error] "; break;
                case LogType.WARNING: header += " [Warning] "; break;
                case LogType.INFO: header += " [Info] "; break;
            }

            try
            {
                await File.AppendAllTextAsync("logs.log", header + logText + "\n");
            }
            catch
            { }
        }
    }
}
