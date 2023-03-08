using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using LauncherApp.Models;

namespace LauncherApp.ViewModels;

internal class MainWindowViewModel : MainWindowViewModelProperties
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
}
