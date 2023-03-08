using System;

namespace LauncherApp.Models;

public class FullScanFileCheckEventArgs : EventArgs
{
    public string? FileName { get; set; }
    public int CurrentFile { get; set; }
    public int TotalFiles { get; set; }

    public FullScanFileCheckEventArgs(string fileName, int currentFile, int totalFiles)
    {
        FileName = fileName;
        CurrentFile = currentFile;
        TotalFiles = totalFiles;
    }
}

public class OnDownloadProgressUpdatedEventArgs : EventArgs
{
    public double? ProgressPercentage { get; set; }

    public OnDownloadProgressUpdatedEventArgs(double progressPercentage)
    {
        ProgressPercentage = progressPercentage;
    }
}

public class OnDownloadRateUpdatedEventArgs : EventArgs
{
    public double? DownloadRate { get; set;}

    public OnDownloadRateUpdatedEventArgs(double downloadRate)
    {
        DownloadRate = downloadRate;
    }
}

public class OnSetUsernameEventArgs : EventArgs
{
    public string? Username { get; set; }

    public OnSetUsernameEventArgs(string username)
    {
        Username = username;
    }
}

public class OnCopyingFilesProgressUpdatedEventArgs : EventArgs
{
    public int CurrentFile { get; set; }
    public int TotalFiles { get; set; }

    public OnCopyingFilesProgressUpdatedEventArgs(int currentFile, int totalFiles)
    {
        CurrentFile = currentFile;
        TotalFiles = totalFiles;
    }
}
