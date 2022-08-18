using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LauncherApp.ViewModels;

internal class MainWindowViewModelProperties : ObservableObject
{
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
