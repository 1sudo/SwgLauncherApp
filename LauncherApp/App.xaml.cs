using System;
using System.IO;
using System.Windows;
using LauncherApp.Models.Properties;

namespace LauncherApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LauncherApp/config.json"))) 
            await ConfigFile.GenerateNewConfig();

        base.OnStartup(e);
    }
}
