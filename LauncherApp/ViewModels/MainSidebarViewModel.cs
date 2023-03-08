using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibLauncherUtil.Properties;
using LibLauncherUtil.gRPC;
using LauncherApp.Models;
using System;

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
    public IRelayCommand EnableDeveloperButton { get; }
    private readonly FileHandler _fileHandler;

    public MainSidebarViewModel()
    {
        _fileHandler = new FileHandler();
        VoteButton = new RelayCommand(() => Trace.WriteLine("Vote button pressed"));
        DonateButton = new RelayCommand(() => Trace.WriteLine("Donate button pressed"));
        ResourcesButton = new RelayCommand(() => Trace.WriteLine("Resources button pressed"));
        BugReportButton = new RelayCommand(() => Trace.WriteLine("Bugreport button pressed"));
        SkillPlannerButton = new RelayCommand(() => Trace.WriteLine("Skillplanner button pressed"));
        SettingsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Settings));
        OptionsButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.OptionsAndMods));
        DeveloperButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Developer));
        EnableDeveloperButton = new RelayCommand(EnableDeveloper);
        PlayButton = new RelayCommand(Play);
        Requests.OnLoggedIn += OnLoggedIn;
        HttpHandler.OnDownloadStarted += OnDownloadStarted;
        HttpHandler.OnDownloadCompleted += OnDownloadCompleted;
        HttpHandler.OnDownloadProgressUpdated += OnDownloadProgressUpdated;
        HttpHandler.OnDownloadRateUpdated += OnDownloadRateUpdated;
        FileHandler.OnFullScanFileCheck += OnFullScanFileCheck;
        FileHandler.OnFullScanStarted += OnFullScanStarted;
        FileHandler.OnFullScanCompleted += OnFullScanCompleted;
        DeveloperButtonVisibility = Visibility.Collapsed;
    }

    private void GetLastSelectedCharacter()
    {
        var config = ConfigFile.GetCurrentServer();

        if (config is not null && !string.IsNullOrEmpty(config.LastSelectedCharacter))
        {
            CharacterList?.ToList().ForEach(character =>
            {
                if (character == config.LastSelectedCharacter)
                {
                    SelectedCharacter = character;
                }
            });
        }
    }

    private void EnableDeveloper()
    {
        if (DeveloperButtonVisibility == Visibility.Collapsed)
        {
            DeveloperButtonVisibility = Visibility.Visible;
        }
        else
        {
            DeveloperButtonVisibility = Visibility.Collapsed;
        }
    }

    private async void Play()
    {
        var config = ConfigFile.GetConfig();
        var currentServer = config?.Servers?[config.ActiveServer];

        if (config is null) return;

        if (_updateAvailable)
        {
            PlayButtonEnabled = false;
            PlayButtonText = "UPDATING";
            await _fileHandler.CheckFilesAsync();
            
            _updateAvailable = false;
        }
        else
        {
            if (SelectedCharacter != "None" && SelectedCharacter is not null)
            {
                if (currentServer is not null)
                {
                    currentServer.LastSelectedCharacter = SelectedCharacter;
                    ConfigFile.SetConfig(config);
                }

                await AppHandler.StartGameAsync(currentServer?.Password ?? "", currentServer?.Username ?? "", SelectedCharacter, true);
            }
            else
            {
                await AppHandler.StartGameAsync(currentServer?.Password ?? "", currentServer?.Username ?? "");
            }
        }
    }

    private async void OnLoggedIn(object? sender, OnLoggedInEventArgs args)
    {
        PlayButtonEnabled = false;
        PlayButtonText = "UPDATE";

        if (args.Characters is not null)
        {
            CharacterList = new ObservableCollection<string>(args.Characters);
        }

        GetLastSelectedCharacter();

        CharacterSelectVisibility = Visibility.Visible;
        DownloadProgressVisibility = Visibility.Collapsed;

        if (await _fileHandler.UpdateIsAvailable())
        {
            _updateAvailable = true;
            PlayButtonText = "UPDATE";
            // OnDownloadCompleted takes care of enabling the button after update
        }
        else
        {
            // Shortcut for enabling play button
            OnDownloadCompleted(this, new EventArgs());
        }

        PlayButtonEnabled = true;
    }

    private void OnDownloadStarted(object? sender, EventArgs args)
    {
        PlayButtonEnabled = false;
        CharacterSelectVisibility = Visibility.Collapsed;
        DownloadProgressVisibility = Visibility.Visible;
        ProgressTextBottomLeft = "Downloading Game Files";
    }

    private void OnDownloadCompleted(object? sender, EventArgs args)
    {
        _updateAvailable = false;
        PlayButtonText = "PLAY";
        PlayButtonEnabled = true;
        CharacterSelectVisibility = Visibility.Visible;
        DownloadProgressVisibility = Visibility.Collapsed;
    }

    private void OnDownloadProgressUpdated(object? sender, OnDownloadProgressUpdatedEventArgs args)
    {
        ProgressBarBottomValue = args.ProgressPercentage;
    }

    private void OnDownloadRateUpdated(object? sender, OnDownloadRateUpdatedEventArgs args)
    {
        ProgressTextBottomRight = args.DownloadRate.ToString() + " Mbps";
    }

    private void OnFullScanFileCheck(object? sender, FullScanFileCheckEventArgs args)
    {
        ProgressTextBottomRight = $"({args.CurrentFile} / {args.TotalFiles})";
        ProgressBarBottomValue = ((double)args.CurrentFile / (double)args.TotalFiles) * 1000;
    }

    private void OnFullScanStarted(object? sender, EventArgs args)
    {
        PlayButtonEnabled = false;
        PlayButtonText = "SCANNING";
        CharacterSelectVisibility = Visibility.Collapsed;
        DownloadProgressVisibility = Visibility.Visible;
        ProgressTextBottomLeft = "Scanning Files... Please Wait...";
    }

    private void OnFullScanCompleted(object? sender, EventArgs args)
    {
        PlayButtonEnabled = true;
        PlayButtonText = "PLAY";
        CharacterSelectVisibility = Visibility.Visible;
        DownloadProgressVisibility = Visibility.Collapsed;
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
    private ObservableCollection<string>? _characterList;
    private string? _selectedCharacter;
    private Visibility? _characterSelectVisibility;
    private Visibility? _downloadProgressVisibility;
    private static Visibility? _developerButtonVisibility;

    public Visibility? DeveloperButtonVisibility 
    {
        get => _developerButtonVisibility;
        set => SetProperty(ref _developerButtonVisibility, value);
    }

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

    public ObservableCollection<string>? CharacterList
    {
        get => _characterList;
        set => SetProperty(ref _characterList, value);
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
