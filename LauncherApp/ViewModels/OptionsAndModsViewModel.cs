using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models;
using System;
using System.Threading.Tasks;

namespace LauncherApp.ViewModels;

internal class OptionsAndModsViewModel : ObservableObject
{
    public IRelayCommand ChangeInstallationDirectoryButton { get; }
    public IAsyncRelayCommand FullScanButton { get; }
    public IRelayCommand SaveChangesButton { get; }
    private readonly FileHandler _fileHandler;

    public OptionsAndModsViewModel()
    {
        _fileHandler= new FileHandler();

        FullScanEnabled = false;
        ChangeInstallationDirectoryButton = new RelayCommand(ChangeInstallationDirectory);
        FullScanButton = new AsyncRelayCommand(FullScan);
        SaveChangesButton = new RelayCommand(SaveChanges);
        FileHandler.UpdateCheckComplete += UpdateCheckComplete;
    }

    private void ChangeInstallationDirectory()
    {

    }

    private async Task FullScan()
    {
        await _fileHandler.CheckFilesAsync(true);
    }

    private void SaveChanges()
    {

    }

    private void UpdateCheckComplete(object? sender, EventArgs args)
    {
        FullScanEnabled = true;
    }

    private string? _installationDirectoryTextbox;
    private int _selectedServer;
    private bool? _fullScanEnabled;

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
}
