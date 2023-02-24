using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models.Handlers;
using LauncherApp.Models.Properties;

namespace LauncherApp.ViewModels;

internal class MainSidebarViewModel : ObservableObject
{
    public IRelayCommand VoteButton { get; }
    public IRelayCommand DonateButton { get; }
    public IRelayCommand ResourcesButton { get; }
    public IRelayCommand BugReportButton { get; }
    public IRelayCommand SkillPlannerButton { get; }
    public IRelayCommand SettingsButton { get; }
    public IRelayCommand OptionsButton { get; }
    public IRelayCommand DeveloperButton { get; }
    public IRelayCommand PlayButton { get; }

    public MainSidebarViewModel()
    {
        VoteButton = new RelayCommand(() => Trace.WriteLine("Vote button pressed"));
        DonateButton = new RelayCommand(() => Trace.WriteLine("Donate button pressed"));
        ResourcesButton = new RelayCommand(() => Trace.WriteLine("Resources button pressed"));
        BugReportButton = new RelayCommand(() => Trace.WriteLine("Bugreport button pressed"));
        SkillPlannerButton = new RelayCommand(() => Trace.WriteLine("Skillplanner button pressed"));
        SettingsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Settings));
        OptionsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.OptionsAndMods));
        DeveloperButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Developer));
        PlayButton = new RelayCommand(Play);
        LibgRPC.Requests.LoggedIn += OnLoggedIn;
        HttpHandler.OnDownloadStarted += OnDownloadStarted;
        HttpHandler.OnDownloadCompleted += OnDownloadCompleted;
        HttpHandler.OnCurrentFileDownloading += OnCurrentFileDownloading;
        HttpHandler.OnDownloadProgressUpdated += OnDownloadProgressUpdated;
    }

    private async void Play()
    {
        var config = ConfigFile.GetCurrentServer();

        if (config is null) return;

        if (_updateAvailable)
        {
            PlayButtonEnabled = false;

            await FileHandler.CheckFilesAsync();

            _updateAvailable = false;
        }
        else
        {
            if (SelectedCharacter != "None" && SelectedCharacter is not null)
            {
                await AppHandler.StartGameAsync(config?.Password ?? "", config?.Username ?? "", SelectedCharacter, true);
            }
            else
            {
                await AppHandler.StartGameAsync(config?.Password ?? "", config?.Username ?? "");
            }
        }
    }

    private async void OnLoggedIn(List<string> characters, string username, bool autoLogin)
    {
        PlayButtonText = "UPDATE";
        PlayButtonEnabled = true;
        CharacterSelectVisibility = Visibility.Visible;
        DownloadProgressVisibility = Visibility.Collapsed;

        if (await FileHandler.UpdateIsAvailable())
        {
            _updateAvailable = true;
            PlayButtonText = "UPDATE";
            // OnDownloadCompleted takes care of enabling the button after update
        }
        else
        {
            // Shortcut for enabling play button
            OnDownloadCompleted();
        }
    }

    private void OnDownloadStarted()
    {
        CharacterSelectVisibility = Visibility.Collapsed;
        DownloadProgressVisibility = Visibility.Visible;
    }

    private void OnDownloadCompleted()
    {
        _updateAvailable = false;
        PlayButtonText = "PLAY";
        PlayButtonEnabled = true;
        CharacterSelectVisibility = Visibility.Visible;
        DownloadProgressVisibility = Visibility.Collapsed;
    }

    private void OnCurrentFileDownloading(string action, string fileName, double currentFile, double totalFiles)
    {

    }

    private void OnDownloadProgressUpdated(long bytesReceived, long totalBytesToReceive, int progressPercentage)
    {
        ProgressBarBottomValue = progressPercentage;
    }

    private bool? _playButtonEnabled;
    private string? _playButtonText;
    private string? _progressTextTopLeft;
    private string? _progressTextTopRight;
    private string? _progressTextBottomLeft;
    private string? _progressTextBottomRight;
    private double? _progressBarTopValue;
    private double? _progressBarBottomValue;
    private bool _updateAvailable;
    private string? _selectedCharacter;
    private Visibility? _characterSelectVisibility;
    private Visibility? _downloadProgressVisibility;

    public bool? PlayButtonEnabled
    {
        get => _playButtonEnabled;
        set => SetProperty(ref _playButtonEnabled, value);
    }

    public string? PlayButtonText
    {
        get => _playButtonText;
        set => SetProperty(ref _playButtonText, value);
    }

    public string? ProgressTextTopLeft
    {
        get => _progressTextTopLeft;
        set => SetProperty(ref _progressTextTopLeft, value);
    }

    public string? ProgressTextTopRight
    {
        get => _progressTextTopRight;
        set => SetProperty(ref _progressTextTopRight, value);
    }

    public string? ProgressTextBottomLeft
    {
        get => _progressTextBottomLeft;
        set => SetProperty(ref _progressTextBottomLeft, value);
    }

    public string? ProgressTextBottomRight
    {
        get => _progressTextBottomRight; 
        set => SetProperty(ref _progressTextBottomRight, value);
    }

    public double? ProgressBarTopValue
    {
        get => _progressBarTopValue; 
        set => SetProperty(ref _progressBarTopValue, value);
    }

    public double? ProgressBarBottomValue
    {
        get => _progressBarBottomValue; 
        set => SetProperty(ref _progressBarBottomValue, value);
    }

    public string? SelectedCharacter
    {
        get => _selectedCharacter;
        set => SetProperty(ref _selectedCharacter, value);
    }

    public Visibility? CharacterSelectVisibility
    {
        get => _characterSelectVisibility;
        set => SetProperty(ref _characterSelectVisibility, value);
    }

    public Visibility? DownloadProgressVisibility
    {
        get => _downloadProgressVisibility;
        set => SetProperty(ref _downloadProgressVisibility, value);
    }
}
