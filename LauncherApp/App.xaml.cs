using System;
using System.IO;
using System.Windows;
using LauncherApp.Models.Properties;

namespace LauncherApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            string _configFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LauncherApp/config.json");

            if (!File.Exists(_configFile))
            {
                await ConfigFile.GenerateNewConfig();
            }
        }
    }
}
