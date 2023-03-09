using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models;

namespace LauncherApp.ViewModels;

internal class MainWindowViewModel : ObservableObject
{
    public IRelayCommand MinimizeButton { get; set; }
    public IRelayCommand CloseButton { get; set; }
    public IRelayCommand UpdatesButton { get; set; }
    public IRelayCommand LogoutButton { get; set; }

    public MainWindowViewModel()
    {
        UpdatesButtonIsEnabled = false;
        MainWindowLogoutButton = Visibility.Collapsed;
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MinimizeButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        CloseButton = new RelayCommand(() => Application.Current.MainWindow!.Close());
        UpdatesButton = new RelayCommand(() => ScreenContainerViewModel.EnableScreen(Screen.Updates));
        LogoutButton = new RelayCommand(OnLogout);
        AccountScreenViewModel.OnSetUsername += OnSetUsername;
    }

    private void OnSetUsername(object? sender, OnSetUsernameEventArgs args)
    {
        UpdatesButtonIsEnabled = true;

        if (args.Username is not null)
        {
            MainWindowUsernameTextBlock = args.Username.ToUpper();
        }

        MainWindowUsernameTextBlockVisibility = Visibility.Visible;
        MainWindowLogoutButton = Visibility.Visible;
    }

    private void OnLogout()
    {
        UpdatesButtonIsEnabled = false;
        MainWindowUsernameTextBlockVisibility = Visibility.Collapsed;
        MainWindowLogoutButton = Visibility.Collapsed;
        ScreenContainerViewModel.EnableScreen(Screen.AccountLogin);
    }

    private string? _mainWindowUsernameTextBlock;
    private Visibility? _mainWindowUsernameTextBlockVisibility;
    private Visibility? _mainWindowLogoutButton;
    private bool? _updatesButtonIsEnabled;

    public string? MainWindowUsernameTextBlock
    {
        get => _mainWindowUsernameTextBlock;
        set => SetProperty(ref _mainWindowUsernameTextBlock, value);
    }

    public Visibility? MainWindowUsernameTextBlockVisibility
    {
        get => _mainWindowUsernameTextBlockVisibility;
        set => SetProperty(ref _mainWindowUsernameTextBlockVisibility, value);
    }

    public Visibility? MainWindowLogoutButton
    {
        get => _mainWindowLogoutButton;
        set => SetProperty(ref _mainWindowLogoutButton, value);
    }

    public bool? UpdatesButtonIsEnabled
    {
        get => _updatesButtonIsEnabled;
        set => SetProperty(ref _updatesButtonIsEnabled, value);
    }
}
