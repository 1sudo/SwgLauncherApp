using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models;
using LibLauncherApp.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace LauncherApp.ViewModels;

internal class OptionsAndModsViewModel : ObservableObject
{
    public IAsyncRelayCommand ChangeInstallationDirectoryButton { get; }
    public IAsyncRelayCommand FullScanButton { get; }
    public IRelayCommand SaveChangesButton { get; }
    private readonly FileHandler _fileHandler;

    public OptionsAndModsViewModel()
    {
        _fileHandler= new FileHandler();
        FullScanEnabled = false;
        ChangeInstallationDirectoryButton = new AsyncRelayCommand(ChangeInstallationDirectory);
        FullScanButton = new AsyncRelayCommand(FullScan);
        SaveChangesButton = new RelayCommand(SaveChanges);
        FileHandler.UpdateCheckComplete += UpdateCheckComplete;

        SetDefaults();
    }

    private void SetDefaults()
    {
        var config = ConfigFile.GetConfig();

        InstallationDirectoryTextbox = config?.Servers?[config.ActiveServer].GameLocation;

        List<string> servers = new();

        if (config?.Servers is not null)
        {
            int i = 0;
            foreach (var server in config.Servers)
            {
                if (server.Value.ServerSelection is not null)
                {
                    servers.Add(server.Value.ServerSelection);
                }
                i++;
            }

            ServerList = new ObservableCollection<string>(servers);

            SelectedServer = config.ActiveServer;
        }
    }

    private async Task ChangeInstallationDirectory()
    {
        var config = ConfigFile.GetConfig();

        if (config is null) return;

        var currentServer = config?.Servers?[config.ActiveServer];

        if (currentServer is null) return;

        using var dialog = new FolderBrowserDialog();

        DialogResult result = dialog.ShowDialog();

        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            string? cachedLocation = null;

            if (!string.IsNullOrEmpty(currentServer.GameLocation))
            {
                cachedLocation = currentServer.GameLocation;
            }

            currentServer.GameLocation = dialog.SelectedPath;
            InstallationDirectoryTextbox = dialog.SelectedPath;

            if (config is not null)
            {
                ConfigFile.SetConfig(config);
                InstallationDirectoryTextbox = currentServer.GameLocation;
            }

            if (!string.IsNullOrEmpty(cachedLocation))
            {
                CopyEligibility canCopy = await _fileHandler.CopyFolder(cachedLocation, currentServer.GameLocation);

                // Don't overwrite by default if directory/files exist. Prompt and ask.
                if (canCopy == CopyEligibility.NOTEMPTY)
                {
                    MessageBoxResult msgResult = System.Windows.MessageBox.Show("The directory you chose is not empty. Do you wish to proceed overwriting any existing files?",
                        "Directory Not Empty", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                    if (msgResult == MessageBoxResult.Yes)
                    {
                        await _fileHandler.CopyFolder(cachedLocation, currentServer.GameLocation, true);
                    }
                    else
                    {
                        // Revert game location
                        currentServer.GameLocation = cachedLocation;
                        InstallationDirectoryTextbox = cachedLocation;

                        if (config is not null)
                        {
                            ConfigFile.SetConfig(config);
                        }

                        return;
                    }
                }
                else if (canCopy == CopyEligibility.SAMEDIR)
                {
                    System.Windows.MessageBox.Show("The directory you chose is already the current game location.",
                        "Same Directory", MessageBoxButton.OK, MessageBoxImage.Information);

                    return;
                }

                ChangeInstallationDirectoryButtonEnabled = false;

                // After copy, scan all files and download anything missing / corrupt
                await FullScan();
            }
        }
    }

    private async Task FullScan()
    {
        await _fileHandler.CheckFilesAsync(true);

        ChangeInstallationDirectoryButtonEnabled = true;
    }

    private void SaveChanges()
    {
        var config = ConfigFile.GetConfig();

        if (config is not null)
        {
            config.ActiveServer = SelectedServer;
            ConfigFile.SetConfig(config);
            System.Windows.Forms.Application.Restart();
            Environment.Exit(0);
        }
    }

    private void UpdateCheckComplete(object? sender, EventArgs args)
    {
        FullScanEnabled = true;
    }

    private string? _installationDirectoryTextbox;
    private int _selectedServer;
    private bool? _fullScanEnabled;
    private bool? _changeInstallationDirectoryButtonEnabled;
    private ObservableCollection<string>? _serverList;

    public string? InstallationDirectoryTextbox
    {
        get => _installationDirectoryTextbox;
        set => SetProperty(ref _installationDirectoryTextbox, value);
    }

    public int SelectedServer
    {
        get => _selectedServer;
        set => SetProperty(ref _selectedServer, value);
    }

    public bool? FullScanEnabled
    {
        get => _fullScanEnabled;
        set => SetProperty(ref _fullScanEnabled, value);
    }

    public bool? ChangeInstallationDirectoryButtonEnabled
    {
        get => _changeInstallationDirectoryButtonEnabled;
        set => SetProperty(ref _changeInstallationDirectoryButtonEnabled, value);
    }

    public ObservableCollection<string>? ServerList 
    { 
        get => _serverList; 
        set => SetProperty(ref _serverList, value);
    }
}
